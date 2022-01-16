using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using System.Threading.Tasks;

namespace HighPolyHeadUpdateRaces
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "HighPolyHead - Update Vanilla Races.esp")
                .Run(args);
        }

        public static readonly ModKey ModKey = ModKey.FromNameAndExtension("High Poly Head.esm");

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {

            if (!state.LoadOrder.ContainsKey(ModKey))
            {
                throw new Exception("You need High Poly Head mod installed for this patch to do anything.");
            }

            // Dictionary containing correlation between vanilla headparts to the HPH equivalent
            Dictionary<FormKey, FormKey> vanilla_to_hph_parts = new Dictionary<FormKey, FormKey>();
            foreach (var hphHeadPart in state.LoadOrder.PriorityOrder.OnlyEnabled().HeadPart().WinningOverrides())
            {
                if (hphHeadPart.EditorID == null || !hphHeadPart.EditorID.StartsWith("00KLH_")) continue;
                // for each HPH record, loop through the vanilla ones again - seems a bit inefficient? compare two lists with LINQ instead?
                foreach (var vanillaHeadPart in state.LoadOrder.PriorityOrder.HeadPart().WinningOverrides())
                {
                    if (vanillaHeadPart.EditorID != null && hphHeadPart.EditorID.EndsWith(vanillaHeadPart.EditorID) && !vanillaHeadPart.EditorID.StartsWith("00KLH_") )
                    {
                        if (!vanilla_to_hph_parts.ContainsKey(vanillaHeadPart.FormKey))
                        {
                            vanilla_to_hph_parts[vanillaHeadPart.FormKey] = hphHeadPart.FormKey;
                        }
                        IHeadPart gimme_head = state.PatchMod.HeadParts.GetOrAddAsOverride(vanillaHeadPart);
                        gimme_head.Flags &= ~HeadPart.Flag.Playable;
                    }
                }
            }

           /*
            * Might cause issues with NPC Overhauls
            * 
            foreach (var race_record in state.LoadOrder.PriorityOrder.OnlyEnabled().Race().WinningOverrides())
            {
                if (race_record.EditorID == null)
                {
                    continue;
                }
                if (race_record.HeadData == null)
                {
                    continue;
                }
                bool has_male_override = false;
                bool has_female_override = false;
                if (race_record.HeadData.Male != null)
                {
                    // male first
                    foreach (var racey_head in race_record.HeadData.Male.HeadParts)
                    {
                        if (racey_head == null)
                        {
                            continue;
                        }
                        if (racey_head.Head.TryResolve(state.LinkCache, out var head2))
                        {
                            if (vanilla_to_hph_parts.ContainsKey(head2.FormKey))
                            {
                                has_male_override = true;
                            }
                        }
                    }
                }
                if (race_record.HeadData.Female != null)
                {
                    foreach (var racey_head in race_record.HeadData.Female.HeadParts)
                    {
                        if (racey_head == null) continue;
                        if (racey_head.Head.TryResolve(state.LinkCache, out var head2))
                        {
                            if (vanilla_to_hph_parts.ContainsKey(head2.FormKey))
                            {
                                has_female_override = true;
                            }
                        }
                    }
                }
                if(!has_female_override && !has_male_override)
                {
                    continue;
                }
                // there are some vanilla heads to override in this race record.
                var race_override = race_record.DeepCopy();
                bool changed = false;
                if (race_override != null && race_override.HeadData != null )
                {
                    if( race_override.HeadData.Female != null && race_override.HeadData.Female.HeadParts != null)
                    {
                        foreach (var racey_head in race_override.HeadData.Female.HeadParts)
                        {
                            if (racey_head.Head.TryResolve(state.LinkCache, out var head2))
                            {
                                if (vanilla_to_hph_parts.ContainsKey(head2.FormKey))
                                {
                                    changed = true;
                                    racey_head.Head.SetTo(vanilla_to_hph_parts[head2.FormKey]);
                                }
                            }
                        }
                    }
                    if (race_override.HeadData.Male != null && race_override.HeadData.Male.HeadParts != null)
                    {
                        foreach (var racey_head in race_override.HeadData.Male.HeadParts)
                        {
                            if (racey_head.Head.TryResolve(state.LinkCache, out var head2))
                            {
                                if (vanilla_to_hph_parts.ContainsKey(head2.FormKey))
                                {
                                    changed = true;
                                    racey_head.Head.SetTo(vanilla_to_hph_parts[head2.FormKey]);
                                }
                            }
                        }
                    }
                }
                if( changed && race_override != null )
                {
                    state.PatchMod.Races.Set(race_override);
                }
            }
            */
            // Now NPC records for preset defaults
            // by now you can tell ive given up on efficiency and just wanted to get the damn thing working
            foreach(var npc_preset in state.LoadOrder.PriorityOrder.OnlyEnabled().Npc().WinningOverrides())
            {
                if (npc_preset != null && npc_preset.EditorID != null)
                {
                    String eid = npc_preset.EditorID;
                    if(eid.Length <= 3)
                    {
                        continue;
                    }
                    string without_last_two = eid.Substring(0, eid.Length - 2);
                    bool needs_override = false;
                    if (without_last_two.EndsWith("Preset"))
                    {
                        for (int index = 0; index < npc_preset.HeadParts.Count; index++)
                        {
                            if (vanilla_to_hph_parts.ContainsKey(npc_preset.HeadParts[index].FormKey))
                            {
                                needs_override = true;
                                break;
                            }
                        }
                    }
                    if (needs_override)
                    {
                        INpc npc_override = state.PatchMod.Npcs.GetOrAddAsOverride(npc_preset);
                        for (int index = 0; index < npc_override.HeadParts.Count; index++)
                        {
                            if (vanilla_to_hph_parts.ContainsKey(npc_override.HeadParts[index].FormKey))
                            {
                                FormKey replacement_head = vanilla_to_hph_parts[npc_override.HeadParts[index].FormKey];
                                IFormLinkGetter<IHeadPartGetter> resolved_head = replacement_head.AsLinkGetter<IHeadPartGetter>();
                                npc_override.HeadParts[index] = resolved_head;
                            }
                        }
                    }
                }
            }
        }
    }
}
