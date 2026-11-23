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
        static readonly float[] AreaGains={.20f,.14f,.12f};
        static readonly float AreaFadeSpeed=Mathf.Sqrt(2f)/3f;
        Vector3 areaMix=Vector3.right;
        float ambienceGain=1f;
        const float KnockGain=.42f, KnockAttack=.08f, KnockRelease=.75f;
        AudioClip rip, steamHiss, detachClip;
        AudioSource detachSource, sheetDetachSource, purchaseSource, archiveRevealSource;
        const float PurchaseGain=.23f;
        const float ArchiveRevealGain=.30f, ArchiveRevealDelay=.22f;
        AudioLowPassFilter peelFilter;
        float soundEnvelope;
        bool soundFadingOut;
        float loopGain, loopAttack, loopRelease, detachDuckGain, detachTail;
        const float DetachDuckHold=.05f, DetachDuckRecovery=.12f;
        PeelStrip soundStrip;
        PeelSettings Settings;

        internal void Initialize(MonoBehaviour context,PeelSettings settings)
        {
            Settings=settings;
            var gameObject=context.gameObject;
            var transform=context.transform;
            var peelLoop=new GameObject("Tool peel loop");
            peelLoop.transform.SetParent(transform,false);
            audioSource=peelLoop.AddComponent<AudioSource>();audioSource.spatialBlend=0;audioSource.playOnAwake=false;
            peelFilter=peelLoop.AddComponent<AudioLowPassFilter>();
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
            purchaseSource=gameObject.AddComponent<AudioSource>();
            purchaseSource.playOnAwake=false;
            purchaseSource.spatialBlend=0f;
            purchaseSource.pitch=.78f;
            purchaseSource.clip=Resources.Load<AudioClip>("BasementSwitchClick");
            archiveRevealSource=gameObject.AddComponent<AudioSource>();
            archiveRevealSource.playOnAwake=false;
            archiveRevealSource.spatialBlend=0f;
            archiveRevealSource.clip=Resources.Load<AudioClip>("ArchiveRevealRustle");
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
            footstepFilter.cutoffFrequency=indoors ? 3200f : 7500f;
            footsteps.pitch=indoors ? .91f : 1f;
            footsteps.PlayOneShot(clip,gain);
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
            detachSource.volume=sheetDetachSource.volume=Settings.SoundVolume;
            purchaseSource.volume=Settings.SoundVolume*PurchaseGain;
            archiveRevealSource.volume=Settings.SoundVolume*ArchiveRevealGain;
            if(footsteps)footsteps.volume=Settings.SoundVolume;
            UpdateAreaVolumes();
        }

        void ApplyPeelVolume()
        {
            float detachDuck=Mathf.Lerp(1f,detachDuckGain,Mathf.Clamp01(detachTail/DetachDuckRecovery));
            audioSource.volume=Settings.SoundVolume*loopGain*soundEnvelope*detachDuck;
        }

        internal void PlayPurchaseConfirmation()
        {
            if(purchaseSource.clip && purchaseSource.volume>0f)purchaseSource.Play();
        }

        internal void PlayArchiveReveal()
        {
            // Keep the rustle separate from the last sheet's detach accent.
            if(archiveRevealSource.clip && archiveRevealSource.volume>0f)archiveRevealSource.PlayDelayed(ArchiveRevealDelay);
        }

        internal void UpdateAreaAmbience(float dt,int area,bool knockActive)
        {
            while(area>0 && !areaSources[area].clip)area--;
            var target=Vector3.zero;
            target[area]=1f;
            areaMix=Vector3.MoveTowards(areaMix,target,dt*AreaFadeSpeed);
            ambienceGain=Mathf.MoveTowards(ambienceGain,knockActive ? KnockGain : 1f,
                dt*(1f-KnockGain)/(knockActive ? KnockAttack : KnockRelease));
            UpdateAreaVolumes();

            foreach(var source in areaSources)
                if(source.clip && source.volume>0f && !source.isPlaying)source.Play();
        }
        void UpdateAreaVolumes()
        {
            if(!Settings)return;
            // Convert area power weights to amplitude for a constant-power crossfade.
            for(int i=0;i<areaSources.Length;i++)
                areaSources[i].volume=Settings.SoundVolume*AreaGains[i]*Mathf.Sqrt(areaMix[i])*ambienceGain;
        }
        internal void BeginPeel(PeelStrip target, int toolLevel, int activeTool, bool steamUnlocked)
        {
            bool usingCard=toolLevel==0;
            bool usingSteam=!usingCard && activeTool==1 && steamUnlocked;
            var clip=usingSteam && steamHiss ? steamHiss : rip;
            if(!audioSource || !clip)return;

            soundStrip=target;
            loopGain=usingSteam && steamHiss ? .16f : usingCard ? .55f : .48f;
            loopAttack=usingSteam ? .065f : usingCard ? .025f : .035f;
            loopRelease=usingSteam ? .145f : usingCard ? .075f : .10f;
            detachDuckGain=usingSteam ? .28f : .55f;
            peelFilter.cutoffFrequency=usingSteam ? 6200f : usingCard ? 7000f : 4500f;
            soundFadingOut=false;
            audioSource.pitch=usingCard ? 1.12f : usingSteam ? .88f : 1f;
            // The steam valve can stay open as the held input moves to a new sheet.
            if(clip==steamHiss && audioSource.clip==clip && audioSource.isPlaying)return;

            audioSource.volume=0;audioSource.Stop();soundEnvelope=0;
            audioSource.clip=clip;
            if(clip==rip)audioSource.timeSamples=Mathf.Clamp(target.SoundSample,0,rip.samples-1);
            audioSource.Play();
        }

        internal void PlayDetach(int detached, bool completedSheet)
        {
            if(detached<=0 || !detachClip || Settings.SoundVolume<=0f)return;
            // Duck before starting the accent, including when steam continues to the next layer.
            detachTail=DetachDuckHold+DetachDuckRecovery;
            ApplyPeelVolume();

            // One accent for this pull, even if several strips pay together.
            var source=completedSheet ? sheetDetachSource : detachSource;
            source.pitch=audioSource.pitch*(completedSheet ? .78f : 1f);
            float minimumGain=completedSheet ? .34f : .2f;
            float strength=Mathf.Lerp(minimumGain,.42f,Mathf.Clamp01((detached-1)/7f));
            source.PlayOneShot(detachClip,strength);
        }

        internal void UpdatePeelSound(float seconds)
        {
            if(!audioSource || !Settings)return;
            float dt=Mathf.Max(0,seconds);
            detachTail=Mathf.Max(0,detachTail-dt);
            float targetVolume=soundFadingOut?0f:(soundStrip?1f:0f);
            if(soundEnvelope!=targetVolume)
            {
                float fade=targetVolume>soundEnvelope ? loopAttack : loopRelease;
                soundEnvelope=Mathf.MoveTowards(soundEnvelope,targetVolume,dt/fade);
            }
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
