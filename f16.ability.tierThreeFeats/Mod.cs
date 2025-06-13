using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using ff16.ability.tierThreeFeats.Template;
using SharedScans.Interfaces;
using System.Diagnostics;

namespace ff16.ability.tierThreeFeats;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    /// Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    public unsafe delegate char HasEquipItemDelegate(long a1, int itemId);
    private HookContainer<HasEquipItemDelegate> _hasEquipItem;

    public delegate char HasDlcDelegate(long a1, uint dlcId);
    private HookContainer<HasDlcDelegate> _hasDLC;

    public unsafe delegate long GetSkillUpgradeLevelDelegate(long a1, uint skillId);
    private WrapperContainer<GetSkillUpgradeLevelDelegate> _getSkillUpgradeLevel;
    private long skillLevelThingyAddress;

    private bool foundDLC = false;
    private bool dlcErrorShown = false;

    private Dictionary<int, uint> reflectionItemToSkill = new Dictionary<int, uint>
    {
        { 100616, 16 }, // Phoenix
        { 100617, 21 }, // Garuda
        { 100618, 29 }, // Ramuh
        { 100619, 34 }, // Titan
        { 100620, 39 }, // Bahamut
        { 100621, 44 }, // Shiva
        { 100622, 49 }, // Odin
    };

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _modConfig = context.ModConfig;


        _logger.WriteLine($"[{_modConfig.ModId}] Initializing...", _logger.ColorGreen);
        var sharedScansController = _modLoader.GetController<ISharedScans>();
        if (sharedScansController == null || !sharedScansController.TryGetTarget(out ISharedScans scans))
        {
            throw new Exception($"[{_modConfig.ModId}] Unable to get ISharedScans!");
        }
        SetupScans(scans);

        var baseAddress = Process.GetCurrentProcess().MainModule!.BaseAddress;
        skillLevelThingyAddress = baseAddress + 0x18170C0;
        _logger.WriteLine($"[{_modConfig.ModId}] Finished Initalization!", _logger.ColorGreen);
    }

    private unsafe void SetupScans(ISharedScans scans)
    {
        scans.AddScan<GetSkillUpgradeLevelDelegate>("48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 20 57 41 56 41 57 48 83 EC 20 48 8B 05 9D");
        scans.AddScan<HasEquipItemDelegate>("48 89 5C 24 ?? 48 89 4C 24 ?? 57 48 83 EC ?? 8B DA");
        scans.AddScan<HasDlcDelegate>("48 89 5C 24 ?? 57 48 83 EC ?? 48 8B 05 ?? ?? ?? ?? 33 DB 8B FA 48 85 C0 75 ?? 48 8B 0D ?? ?? ?? ?? 48 85 C9 0F 84");

        _getSkillUpgradeLevel = scans.CreateWrapper<GetSkillUpgradeLevelDelegate>(_modConfig.ModId);
        _hasEquipItem = scans.CreateHook<HasEquipItemDelegate>(HasEquipItemImpl, _modConfig.ModId);
        _hasDLC = scans.CreateHook<HasDlcDelegate>(HasDlcImpl, _modConfig.ModId);
    }

    private unsafe char HasEquipItemImpl(long a1, int itemId)
    {
        bool skillMastered = false;
        bool itemEquipped = _hasEquipItem.Hook!.OriginalFunction(a1, itemId) == 1;
        if (reflectionItemToSkill.TryGetValue(itemId, out uint skillId))
        {
            skillMastered = foundDLC && (_getSkillUpgradeLevel.Wrapper.Invoke(*(long*)skillLevelThingyAddress, skillId) == 3);
        }

        return (char)((itemEquipped || skillMastered) ? 1 : 0);
    }

    private char HasDlcImpl(long a1, uint dlcId)
    {
        var res = _hasDLC.Hook!.OriginalFunction(a1, dlcId);

        if (dlcId == 3)
        {
            foundDLC = res == 1;
            if (!foundDLC && !dlcErrorShown)
            {
                _logger.WriteLine($"[{_modConfig.ModId}] Rising Tide DLC not found, the mod will not work!", _logger.ColorRed);
                dlcErrorShown = true;
            }
        }

        return res;
    }


    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}