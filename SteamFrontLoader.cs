using System;
using System.Collections.Generic;
using System.ComponentModel;
using Eco.Core.Items;
using Eco.Gameplay.Components;
using Eco.Gameplay.Components.Auth;
using Eco.Gameplay.Components.VehicleModules;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.DynamicValues;
using Eco.Gameplay.Interactions;
using Eco.Gameplay.Items;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Occupancy;
using Eco.Gameplay.Players;
using Eco.Gameplay.Skills;
using Eco.Gameplay.Systems.TextLinks;
using Eco.Shared.Math;
using Eco.Shared.Networking;
using Eco.Shared.Localization;
using Eco.Shared.Serialization;
using Eco.Shared.Utils;
using Eco.Shared.Items;
using Eco.Gameplay.Systems.NewTooltip;
using Eco.Core.Controller;
using Eco.Gameplay.Components.Storage;
using Eco.Core.Utils;
using Eco.World.Blocks;
using Eco.World;
using Vector3 = System.Numerics.Vector3;
using System.Linq;

namespace Eco.Mods.TechTree
{
    [Serialized]
    [LocDisplayName("Steam Front Loader")]
    [Weight(20000)]
    [AirPollution(0.5f)]
    [Tag("Excavation")]
    [Ecopedia("Crafted Objects", "Vehicles")]
    public partial class SteamFrontLoaderItem : WorldObjectItem<SteamFrontLoaderObject>
    {
        public override LocString DisplayDescription { get { return Localizer.DoStr("Small scale bucket loader. Great for flat to low slope excavation."); } }
    }

    [RequiresSkill(typeof(MechanicsSkill), 2)]
    public partial class SteamFrontLoaderRecipe : RecipeFamily
    {
        public SteamFrontLoaderRecipe()
        {
var recipe = new Recipe();
            recipe.Init(
                name: "SteamFrontLoader",  //noloc
                displayName: Localizer.DoStr("Steam Front Loader"),

                // Defines the ingredients needed to craft this recipe. An ingredient items takes the following inputs
                // type of the item, the amount of the item, the skill required, and the talent used.
                ingredients: new List<IngredientElement>
                {
                    new IngredientElement(typeof(IronPlateItem), 20, typeof(MechanicsSkill), typeof(MechanicsLavishResourcesTalent)),
                new IngredientElement(typeof(IronPipeItem), 8, typeof(MechanicsSkill), typeof(MechanicsLavishResourcesTalent)),
                new IngredientElement(typeof(ScrewsItem), 50, typeof(MechanicsSkill), typeof(MechanicsLavishResourcesTalent)),
                new IngredientElement(typeof(LeatherHideItem), 20, typeof(MechanicsSkill), typeof(MechanicsLavishResourcesTalent)),
                new IngredientElement("Lumber", 30, typeof(MechanicsSkill), typeof(MechanicsLavishResourcesTalent)),
                new IngredientElement(typeof(PortableSteamEngineItem), 1, true),
                new IngredientElement(typeof(IronWheelItem), 4, true),
                new IngredientElement(typeof(IronAxleItem), 2, true)
                },

                // Define our recipe output items.
                // For every output item there needs to be one CraftingElement entry with the type of the final item and the amount
                // to create.
                items: new List<CraftingElement>
                {
                    new CraftingElement<SteamFrontLoaderItem>()
                });
            this.Recipes = new List<Recipe> { recipe };
            this.ExperienceOnCraft = 25; // Defines how much experience is gained when crafted.
            
            // Defines the amount of labor required and the required skill to add labor
            this.LaborInCalories = CreateLaborInCaloriesValue(1250, typeof(MechanicsSkill));

            // Defines our crafting time for the recipe
            this.CraftMinutes = CreateCraftTimeValue(beneficiary: typeof(SteamFrontLoaderRecipe), start: 10, skillType: typeof(MechanicsSkill));

            // Perform pre/post initialization for user mods and initialize our recipe instance with the display name "Skid Steer"
            this.ModsPreInitialize();
            this.Initialize(displayText: Localizer.DoStr("Steam Front Loader"), recipeType: typeof(SteamFrontLoaderRecipe));
            this.ModsPostInitialize();

            // Register our RecipeFamily instance with the crafting system so it can be crafted.
            CraftingComponent.AddRecipe(tableType: typeof(AssemblyLineObject), recipe: this);
        }

        /// <summary>Hook for mods to customize RecipeFamily before initialization. You can change recipes, xp, labor, time here.</summary>
        partial void ModsPreInitialize();

        /// <summary>Hook for mods to customize RecipeFamily after initialization, but before registration. You can change skill requirements here.</summary>
        partial void ModsPostInitialize();
    }



    public class SteamFrontLoaderUtilities
    {
        // Mapping for custom stack sizes in vehicles by vehicle type as key
        // We can have different stack sizes in different vehicles with this
        public static Dictionary<Type, StackLimitTypeRestriction> AdvancedVehicleStackSizeMap = new Dictionary<Type, StackLimitTypeRestriction>();

        static SteamFrontLoaderUtilities() => CreateBlockStackSizeMaps();

        private static void CreateBlockStackSizeMaps()
        {
            var blockItems = Item.AllItems.Where(x => x is BlockItem).Cast<BlockItem>().ToList();

            // SteamFrontLoader
            var SteamFrontLoaderMap = new StackLimitTypeRestriction(true, 30);

            SteamFrontLoaderMap.AddListRestriction(blockItems.GetItemsByBlockAttribute<Diggable>(), 30);
            SteamFrontLoaderMap.AddListRestriction(blockItems.GetItemsByBlockAttribute<Minable>(), 30);


            // SteamFrontLoader
            AdvancedVehicleStackSizeMap.Add(typeof(SteamFrontLoaderObject), SteamFrontLoaderMap);
        }

        public static StackLimitTypeRestriction GetInventoryRestriction(object obj) => AdvancedVehicleStackSizeMap.GetOrDefault(obj.GetType());
    }

    [Serialized]
   [RequireComponent(typeof(StandaloneAuthComponent))]
    [RequireComponent(typeof(FuelSupplyComponent))]
    [RequireComponent(typeof(FuelConsumptionComponent))]
    [RequireComponent(typeof(MovableLinkComponent))]
    [RequireComponent(typeof(AirPollutionComponent))]
    [RequireComponent(typeof(VehicleComponent))]
    [RequireComponent(typeof(CustomTextComponent))]
    [RequireComponent(typeof(VehicleToolComponent))]
    [RequireComponent(typeof(MinimapComponent))]  
    public class SteamFrontLoaderObject : PhysicsWorldObject, IRepresentsItem
    {
        static SteamFrontLoaderObject()
        {
            WorldObject.AddOccupancy<SteamFrontLoaderObject>(new List<BlockOccupancy>(0));
        }
        public override TableTextureMode TableTexture => TableTextureMode.Metal;
        public override bool PlacesBlocks            => false;
        public override LocString DisplayName { get { return Localizer.DoStr("Steam Front Loader"); } }
        public Type RepresentedItemType { get { return typeof(SteamFrontLoaderItem); } }

        private static string[] fuelTagList = new string[]
        {
            "Burnable Fuel"
        };
        private SteamFrontLoaderObject() { }
        protected override void Initialize()
        {
            base.Initialize();

            GetComponent<CustomTextComponent>().Initialize(30);
            this.GetComponent<FuelSupplyComponent>().Initialize(2, fuelTagList);
            this.GetComponent<FuelConsumptionComponent>().Initialize(25);
            this.GetComponent<AirPollutionComponent>().Initialize(0.5f);
            this.GetComponent<VehicleComponent>().Initialize(12, 1.2f, 1); // Or this.GetComponent<VehicleComponent>().HumanPowered(2);
            this.GetComponent<VehicleToolComponent>().Initialize(4, 2800000, new DirtItem(),
                100, 200, 0, SteamFrontLoaderUtilities.GetInventoryRestriction(this), toolOnMount: true);
        }
    }
}