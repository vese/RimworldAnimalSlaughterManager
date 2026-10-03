using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using RimWorld;
using Verse;

namespace ASM;

    public class PresetEntry
    {
        public string name;
        public string path;
        public DateTime date;
        public PresetScope scope;
        public CondBucket? condBucket;

        /// <summary>Rank for delete-gating: a context may only delete presets at or below its own rank.</summary>
        public int Rank => scope == PresetScope.All ? 3 : scope == PresetScope.Kind ? 2 : 1;

        /// <summary>True if a management context of <paramref name="contextRank"/> may delete this preset.</summary>
        public bool DeletableFrom(int contextRank) => Rank <= contextRank;
    }
