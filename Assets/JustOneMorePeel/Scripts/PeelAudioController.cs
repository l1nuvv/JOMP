using UnityEngine;

namespace JustOneMorePeel
{
    internal sealed class PeelAudioController
    {
        AudioSource audioSource;
        AudioSource footsteps;
        AudioLowPassFilter footstepFilter;
        AudioClip[] footstepClips;
        AudioClip landingClip;
        Vector3 previousStepPosition;
        float stepDistance;
        int nextFootstep;
        bool hasStepPosition, wasGrounded;
        float airbornePeak;
        const float StepSpacing=1.65f;
        readonly AudioSource[] areaSources=new AudioSource[3];
        static readonly float[] AreaGains={.20f,.20f,.14f};
        static readonly float AreaFadeSpeed=Mathf.Sqrt(2f)/3f;
        Vector3 areaMix=Vector3.right;
        float courtyardGain=1f;
        const float CourtyardKnockGain=.42f, KnockAttack=.08f, KnockRelease=.75f;
        AudioClip rip, steamHiss, detachClip;
        AudioSource detachSource, sheetDetachSource;
        float soundEnvelope;
        bool soundFadingOut;
        const float SoundFadeIn=.04f, SoundFadeOut=.12f;
        PeelStrip soundStrip;
        PeelSettings Settings;

        internal void Initialize(MonoBehaviour context,PeelSettings settings)
        {
            Settings=settings;
            var gameObject=context.gameObject;
            var transform=context.transform;
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.spatialBlend=0;audioSource.playOnAwake=false;
            rip=Resources.Load<AudioClip>("PaperRustleCandidate");
            if(!rip)Debug.LogWarning("PeelGame: paper sound candidate missing",context);
            steamHiss=Resources.Load<AudioClip>("SteamHiss");
            if(!steamHiss)Debug.LogWarning("PeelGame: steam hiss missing; using paper rustle",context);
            audioSource.clip=rip;audioSource.loop=true;
            detachClip=Resources.Load<AudioClip>("PaperDetach");
            detachSource=gameObject.AddComponent<AudioSource>();
            detachSource.playOnAwake=false;
            sheetDetachSource=gameObject.AddComponent<AudioSource>();
            sheetDetachSource.playOnAwake=false;
            var paperFilter=gameObject.AddComponent<AudioLowPassFilter>();paperFilter.cutoffFrequency=7000f;
            var ambience=new GameObject("Chapter ambience");
            ambience.transform.SetParent(transform,false);
            areaSources[0]=CreateAreaLoop(ambience,"ForgetMeNotCandidate");
            areaSources[1]=CreateAreaLoop(ambience,"BasementMusicCandidate");
            areaSources[2]=CreateAreaLoop(ambience,"ArchiveRoomTone");
            var stepBank=Resources.Load<PeelFootstepSounds>("FootstepSounds");
            if(stepBank && stepBank.Clips!=null && stepBank.Clips.Length>0)
            {
                footstepClips=stepBank.Clips;
                landingClip=stepBank.Landing;
                var stepObject=new GameObject("Player footsteps");
                stepObject.transform.SetParent(transform,false);
                footsteps=stepObject.AddComponent<AudioSource>();
                footsteps.playOnAwake=false;
                footstepFilter=stepObject.AddComponent<AudioLowPassFilter>();
            }
        }

        internal void UpdateFootsteps(Vector3 position,bool movementActive,bool grounded,bool indoors)
        {
            if(!footsteps)return;

            var delta=position-previousStepPosition;
            previousStepPosition=position;
            if(!movementActive || !hasStepPosition || delta.sqrMagnitude>4f)
            {
                hasStepPosition=movementActive;
                wasGrounded=grounded;
                airbornePeak=position.y;
                stepDistance=0f;
                return;
            }
            if(!grounded)
            {
                airbornePeak=wasGrounded ? Mathf.Max(position.y,position.y-delta.y) : Mathf.Max(airbornePeak,position.y);
                wasGrounded=false;
                stepDistance=0f;
                return;
            }
            if(!wasGrounded)
            {
                wasGrounded=true;
                float drop=airbornePeak-position.y;
                if(drop>=.24f)PlayMovementSound(landingClip,indoors,Mathf.Lerp(.24f,.42f,Mathf.Clamp01(drop/1.2f)));
                return;
            }

            delta.y=0f;
            stepDistance+=delta.magnitude;
            if(stepDistance<StepSpacing)return;
            stepDistance%=StepSpacing;

            var clip=footstepClips[nextFootstep];
            nextFootstep=(nextFootstep+1)%footstepClips.Length;
            PlayMovementSound(clip,indoors,indoors ? .25f : .32f);
        }

        void PlayMovementSound(AudioClip clip,bool indoors,float gain)
        {
            if(!clip || Settings.SoundVolume<=0f)return;
            footstepFilter.cutoffFrequency=indoors ? 2700f : 7500f;
            footsteps.pitch=indoors ? .91f : 1f;
            footsteps.PlayOneShot(clip,Settings.SoundVolume*gain);
        }

        static AudioSource CreateAreaLoop(GameObject parent,string clipName)
        {
            var source=parent.AddComponent<AudioSource>();
            source.spatialBlend=0f;
            source.loop=true;
            source.playOnAwake=false;
            source.volume=0f;
            source.clip=Resources.Load<AudioClip>(clipName);
            if(!source.clip)Debug.LogWarning($"PeelGame: missing chapter ambience {clipName}",parent);
            return source;
        }

        internal void ApplySettings()
        {
            ApplyPeelVolume();
            UpdateAreaVolumes();
        }

        void ApplyPeelVolume()
        {
            if(audioSource)
                audioSource.volume=Settings.SoundVolume*(steamHiss && audioSource.clip==steamHiss ? .28f : .55f)*soundEnvelope;
        }

        internal void UpdateAreaAmbience(float dt,int area,bool courtyardKnock)
        {
            while(area>0 && !areaSources[area].clip)area--;
            var target=Vector3.zero;
            target[area]=1f;
            areaMix=Vector3.MoveTowards(areaMix,target,dt*AreaFadeSpeed);
            bool duckCourtyard=area==0 && courtyardKnock;
            courtyardGain=Mathf.MoveTowards(courtyardGain,duckCourtyard ? CourtyardKnockGain : 1f,
                dt*(1f-CourtyardKnockGain)/(duckCourtyard ? KnockAttack : KnockRelease));
            UpdateAreaVolumes();

            foreach(var source in areaSources)
                if(source.clip && source.volume>0f && !source.isPlaying)source.Play();
        }
        void UpdateAreaVolumes()
        {
            if(!Settings)return;
            for(int i=0;i<areaSources.Length;i++)
                areaSources[i].volume=Settings.SoundVolume*AreaGains[i]*areaMix[i]*(i==0 ? courtyardGain : 1f);
        }
        internal void BeginPeel(PeelStrip target, int toolLevel, int activeTool, bool steamUnlocked)
        {
            soundStrip=target;
            bool usingSteam=toolLevel>0 && activeTool==1 && steamUnlocked;
            var clip=usingSteam && steamHiss ? steamHiss : rip;
            if(!audioSource || !clip)return;

            soundFadingOut=false;
            audioSource.pitch=toolLevel==0 ? 1.12f : usingSteam ? .88f : 1f;
            // The steam valve can stay open as the held input moves to a new sheet.
            if(clip==steamHiss && audioSource.clip==clip && audioSource.isPlaying)return;

            audioSource.volume=0;audioSource.Stop();soundEnvelope=0;
            audioSource.clip=clip;
            if(clip==rip)audioSource.timeSamples=Mathf.Clamp(target.SoundSample,0,rip.samples-1);
            audioSource.Play();
        }

        internal void PlayDetach(int detached, bool completedSheet)
        {
            if(detached>0 && detachClip && Settings.SoundVolume>0f)
            {
                // One accent for this pull, even if several strips pay together.
                var source=completedSheet ? sheetDetachSource : detachSource;
                source.pitch=audioSource.pitch*(completedSheet ? .78f : 1f);
                float minimumGain=completedSheet ? .34f : .2f;
                float strength=Mathf.Lerp(minimumGain,.42f,Mathf.Clamp01((detached-1)/7f));
                source.PlayOneShot(detachClip,Settings.SoundVolume*strength);
            }
        }

        internal void UpdatePeelSound(float seconds)
        {
            if(!audioSource || !Settings)return;
            float targetVolume=soundFadingOut?0f:(soundStrip?1f:0f);
            float fade=targetVolume>soundEnvelope?SoundFadeIn:SoundFadeOut;
            soundEnvelope=Mathf.MoveTowards(soundEnvelope,targetVolume,Mathf.Max(0,seconds)/fade);
            ApplyPeelVolume();
            if(soundFadingOut && soundEnvelope<=0f){audioSource.Stop();soundFadingOut=false;}
        }
        internal void StopPeelSound()
        {
            if(audioSource && soundStrip)
            {
                if(audioSource.isPlaying && audioSource.clip==rip) soundStrip.SoundSample=audioSource.timeSamples;
                soundFadingOut=audioSource.isPlaying;
            }
            soundStrip=null;
        }
    }
}
