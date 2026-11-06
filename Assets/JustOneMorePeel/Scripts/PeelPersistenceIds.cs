using System.Collections.Generic;

namespace JustOneMorePeel
{
    // Never infer a saved stack from its name, position or enumeration order.
    public static class PeelPersistenceIds
    {
        public static bool TryIndex(IEnumerable<PeelPosterStack> stacks,
            out Dictionary<string, PeelPosterStack> index, out string error)
        {
            index=new Dictionary<string, PeelPosterStack>();
            error=null;
            foreach(var stack in stacks)
            {
                if(!stack || string.IsNullOrWhiteSpace(stack.PersistentId))
                {
                    error="A poster stack has no persistent ID.";
                    index.Clear();return false;
                }
                if(index.ContainsKey(stack.PersistentId))
                {
                    error="Duplicate poster stack ID: "+stack.PersistentId;
                    index.Clear();return false;
                }
                index.Add(stack.PersistentId,stack);
            }
            return true;
        }
    }
}
