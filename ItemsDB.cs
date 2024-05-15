using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class ItemsDB
        {
            public static List<Item> getList()
            {
                List<Item> items = new List<Item>();

                 // Ore
                 items.Add(new Item("Stone", "Камень", "MyObjectBuilder_Ore/Stone"));
                 items.Add(new Item("Iron Ore", "Железная руда", "MyObjectBuilder_Ore/Iron"));
                 items.Add(new Item("Nickel Ore", "Никелевая руда", "MyObjectBuilder_Ore/Nickel"));
                 items.Add(new Item("Cobalt Ore", "Кобальтовая руда", "MyObjectBuilder_Ore/Cobalt"));
                 items.Add(new Item("Magnesium Ore", "Магниевая руда", "MyObjectBuilder_Ore/Magnesium"));
                 items.Add(new Item("Silicon Ore", "Кремниевая руда", "MyObjectBuilder_Ore/Silicon"));
                 items.Add(new Item("Silver Ore", "Серебряная руда", "MyObjectBuilder_Ore/Silver"));
                 items.Add(new Item("Gold Ore", "Золотая руда", "MyObjectBuilder_Ore/Gold"));
                 items.Add(new Item("Platinum Ore", "Платиновая руда", "MyObjectBuilder_Ore/Platinum"));
                 items.Add(new Item("Uranium Ore", "Урановая руда", "MyObjectBuilder_Ore/Uranium"));
                 items.Add(new Item("Scrap Metal", "Металлолом", "MyObjectBuilder_Ore/Scrap"));
                 items.Add(new Item("Ice", "Лед", "MyObjectBuilder_Ore/Ice"));

                 // Ingots
                 items.Add(new Item("Gravel", "Гравий", "MyObjectBuilder_Ingot/Stone", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/HydrogenBottlesRefill", "0.9"}, {"MyObjectBuilder_BlueprintDefinition/IceToOxygen", "0.9"}, {"MyObjectBuilder_BlueprintDefinition/Position0010_StoneOreToIngotBasic", "1.4"}, {"MyObjectBuilder_BlueprintDefinition/StoneOreToIngot", "14"}, {"MyObjectBuilder_BlueprintDefinition/StoneOreToIngot_Deconstruction", "1"}}));
                 items.Add(new Item("Iron Ingot", "Железный слиток", "MyObjectBuilder_Ingot/Iron", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/IronOreToIngot", "0.7"}, {"MyObjectBuilder_BlueprintDefinition/ScrapIngotToIronIngot", "0.8"}, {"MyObjectBuilder_BlueprintDefinition/ScrapToIronIngot", "0.8"}}));
                 items.Add(new Item("Nickel Ingot", "Никелевый слиток", "MyObjectBuilder_Ingot/Nickel", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/NickelOreToIngot", "0.4"}}));
                 items.Add(new Item("Cobalt Ingot", "Кобальтовый слиток", "MyObjectBuilder_Ingot/Cobalt", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/CobaltOreToIngot", "0.3"}}));
                 items.Add(new Item("Magnesium Powder", "Магниевый слиток", "MyObjectBuilder_Ingot/Magnesium", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/MagnesiumOreToIngot", "0.007"}}));
                 items.Add(new Item("Silicon Wafer", "Кремниевая пластина", "MyObjectBuilder_Ingot/Silicon", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/SiliconOreToIngot", "0.7"}}));
                 items.Add(new Item("Silver Ingot", "Серебряный слиток", "MyObjectBuilder_Ingot/Silver", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/SilverOreToIngot", "0.1"}}));
                 items.Add(new Item("Gold Ingot", "Золотой слиток", "MyObjectBuilder_Ingot/Gold", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/GoldOreToIngot", "0.01"}}));
                 items.Add(new Item("Platinum Ingot", "Платиновый слиток", "MyObjectBuilder_Ingot/Platinum", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/PlatinumOreToIngot", "0.005"}}));
                 items.Add(new Item("Uranium Ingot", "Урановый слиток", "MyObjectBuilder_Ingot/Uranium", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/UraniumOreToIngot", "0.01"}}));
                 items.Add(new Item("Old Scrap Metal", "Старый металлолом", "MyObjectBuilder_Ingot/Scrap"));

                 // Components
                 items.Add(new Item("Construction Comp.", "Строительные компоненты", "MyObjectBuilder_Component/Construction", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/ConstructionComponent", "1"}}));
                 items.Add(new Item("Metal Grid", "Компонент решётки", "MyObjectBuilder_Component/MetalGrid", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/MetalGrid", "1"}}));
                 items.Add(new Item("Interior Plate", "Внутренняя пластина", "MyObjectBuilder_Component/InteriorPlate", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/InteriorPlate", "1"}}));
                 items.Add(new Item("Steel Plate", "Стальная пластина", "MyObjectBuilder_Component/SteelPlate", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/SteelPlate", "1"}}));
                 items.Add(new Item("Girder", "Балка", "MyObjectBuilder_Component/Girder", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/GirderComponent", "1"}}));
                 items.Add(new Item("Small Steel Tube", "Малая трубка", "MyObjectBuilder_Component/SmallTube", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/SmallTube", "1"}}));
                 items.Add(new Item("Large Steel Tube", "Большая стальная труба", "MyObjectBuilder_Component/LargeTube", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/LargeTube", "1"}}));
                 items.Add(new Item("Motor", "Мотор", "MyObjectBuilder_Component/Motor", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/MotorComponent", "1"}}));
                 items.Add(new Item("Display", "Экран", "MyObjectBuilder_Component/Display", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Display", "1"}}));
                 items.Add(new Item("Bulletproof Glass", "Бронированное стекло", "MyObjectBuilder_Component/BulletproofGlass", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/BulletproofGlass", "1"}}));
                 items.Add(new Item("Superconductor", "Сверхпроводник", "MyObjectBuilder_Component/Superconductor", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Superconductor", "1"}}));
                 items.Add(new Item("Computer", "Компьютер", "MyObjectBuilder_Component/Computer", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/ComputerComponent", "1"}}));
                 items.Add(new Item("Reactor Comp.", "Компоненты реактора", "MyObjectBuilder_Component/Reactor", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/ReactorComponent", "1"}}));
                 items.Add(new Item("Thruster Comp.", "Детали ионного ускорителя", "MyObjectBuilder_Component/Thrust", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/ThrustComponent", "1"}}));
                 items.Add(new Item("Gravity Comp.", "Компоненты гравитационного генератора", "MyObjectBuilder_Component/GravityGenerator", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/GravityGeneratorComponent", "1"}}));
                 items.Add(new Item("Medical Comp.", "Медицинские компоненты", "MyObjectBuilder_Component/Medical", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/MedicalComponent", "1"}}));
                 items.Add(new Item("Radio-comm Comp.", "Радиокомпоненты", "MyObjectBuilder_Component/RadioCommunication", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/RadioCommunicationComponent", "1"}}));
                 items.Add(new Item("Detector Comp.", "Компоненты детектора", "MyObjectBuilder_Component/Detector", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/DetectorComponent", "1"}}));
                 items.Add(new Item("Explosives", "Взрывчатка", "MyObjectBuilder_Component/Explosives", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/ExplosivesComponent", "1"}}));
                 items.Add(new Item("Solar Cell", "Солнечная ячейка", "MyObjectBuilder_Component/SolarCell", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/SolarCell", "1"}}));
                 items.Add(new Item("Power Cell", "Энергоячейка", "MyObjectBuilder_Component/PowerCell", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/PowerCell", "1"}}));

                 // Bottles
                 items.Add(new Item("Oxygen Bottle", "Кислородный баллон", "MyObjectBuilder_OxygenContainerObject/OxygenBottle", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0010_OxygenBottle", "1"}}));
                 items.Add(new Item("Hydrogen Bottle", "Водородный баллон", "MyObjectBuilder_GasContainerObject/HydrogenBottle", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0020_HydrogenBottle", "1"}}));

                 // Tools
                 items.Add(new Item("GoodAI Bot Feedback", "Отзывы о ботах GoodAI", "MyObjectBuilder_PhysicalGunObject/GoodAIRewardPunishmentTool"));
                 items.Add(new Item("S-10 Pistol", "Пистолет S-10", "MyObjectBuilder_PhysicalGunObject/SemiAutoPistolItem", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0010_SemiAutoPistol", "1"}}));
                 items.Add(new Item("S-20A Pistol", "Пистолет S-20A", "MyObjectBuilder_PhysicalGunObject/FullAutoPistolItem", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0020_FullAutoPistol", "1"}}));
                 items.Add(new Item("S-10E Pistol", "Пистолет S-10E", "MyObjectBuilder_PhysicalGunObject/ElitePistolItem", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0030_EliteAutoPistol", "1"}}));
                 items.Add(new Item("Flare Gun", "Ракетница", "MyObjectBuilder_PhysicalGunObject/FlareGunItem", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0005_FlareGun", "1"}}));
                 items.Add(new Item("MR-20 Rifle", "Винтовка MR-20", "MyObjectBuilder_PhysicalGunObject/AutomaticRifleItem", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0040_AutomaticRifle", "1"}}));
                 items.Add(new Item("MR-8P Rifle", "Винтовка MR-8P", "MyObjectBuilder_PhysicalGunObject/PreciseAutomaticRifleItem", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0060_PreciseAutomaticRifle", "1"}}));
                 items.Add(new Item("MR-50A Rifle", "Винтовка MR-50A", "MyObjectBuilder_PhysicalGunObject/RapidFireAutomaticRifleItem", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0050_RapidFireAutomaticRifle", "1"}}));
                 items.Add(new Item("MR-30E Rifle", "Винтовка MR-30E", "MyObjectBuilder_PhysicalGunObject/UltimateAutomaticRifleItem", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0070_UltimateAutomaticRifle", "1"}}));
                 items.Add(new Item("RO-1 Rocket Launcher", "Ракетница RO-1", "MyObjectBuilder_PhysicalGunObject/BasicHandHeldLauncherItem", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0080_BasicHandHeldLauncher", "1"}}));
                 items.Add(new Item("PRO-1 Rocket Launcher", "Ракетница PRO-1", "MyObjectBuilder_PhysicalGunObject/AdvancedHandHeldLauncherItem", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0090_AdvancedHandHeldLauncher", "1"}}));
                 items.Add(new Item("Welder", "Сварщик", "MyObjectBuilder_PhysicalGunObject/WelderItem", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0090_Welder", "1"}}));
                 items.Add(new Item("Enhanced Welder", "Улучшенный сварщик", "MyObjectBuilder_PhysicalGunObject/Welder2Item", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0100_Welder2", "1"}}));
                 items.Add(new Item("Proficient Welder", "Продвинутый сварщик", "MyObjectBuilder_PhysicalGunObject/Welder3Item", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0110_Welder3", "1"}}));
                 items.Add(new Item("Elite Welder", "Элитный сварщик", "MyObjectBuilder_PhysicalGunObject/Welder4Item", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0120_Welder4", "1"}}));
                 items.Add(new Item("Grinder", "Резак", "MyObjectBuilder_PhysicalGunObject/AngleGrinderItem", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0010_AngleGrinder", "1"}}));
                 items.Add(new Item("Enhanced Grinder", "Улучшенная болгарка", "MyObjectBuilder_PhysicalGunObject/AngleGrinder2Item", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0020_AngleGrinder2", "1"}}));
                 items.Add(new Item("Proficient Grinder", "Продвинутая болгарка", "MyObjectBuilder_PhysicalGunObject/AngleGrinder3Item", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0030_AngleGrinder3", "1"}}));
                 items.Add(new Item("Elite Grinder", "Элитная болгарка", "MyObjectBuilder_PhysicalGunObject/AngleGrinder4Item", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0040_AngleGrinder4", "1"}}));
                 items.Add(new Item("Hand Drill", "Ручной бур", "MyObjectBuilder_PhysicalGunObject/HandDrillItem", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0050_HandDrill", "1"}}));
                 items.Add(new Item("Enhanced Hand Drill", "Улучшенный ручной бур", "MyObjectBuilder_PhysicalGunObject/HandDrill2Item", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0060_HandDrill2", "1"}}));
                 items.Add(new Item("Proficient Hand Drill", "Продвинутый ручной бур", "MyObjectBuilder_PhysicalGunObject/HandDrill3Item", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0070_HandDrill3", "1"}}));
                 items.Add(new Item("Elite Hand Drill", "Элитный ручной бур", "MyObjectBuilder_PhysicalGunObject/HandDrill4Item", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0080_HandDrill4", "1"}}));
                 items.Add(new Item("CubePlacer", "CubePlacer", "MyObjectBuilder_PhysicalGunObject/CubePlacerItem"));

                 // Ammo
                 items.Add(new Item("S-10 Pistol Magazine", "Магазин пистолета S-10", "MyObjectBuilder_AmmoMagazine/SemiAutoPistolMagazine", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0010_SemiAutoPistolMagazine", "1"}}));
                 items.Add(new Item("S-20A Pistol Magazine", "Магазин пистолета S-20A", "MyObjectBuilder_AmmoMagazine/FullAutoPistolMagazine", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0020_FullAutoPistolMagazine", "1"}}));
                 items.Add(new Item("S-10E Pistol Magazine", "Магазин пистолета S-10E", "MyObjectBuilder_AmmoMagazine/ElitePistolMagazine", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0030_ElitePistolMagazine", "1"}}));
                 items.Add(new Item("Flare Gun Clip", "Магазин для ракетницы", "MyObjectBuilder_AmmoMagazine/FlareClip", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0005_FlareGunMagazine", "1"}}));
                 items.Add(new Item("Fireworks Blue", "Фейерверк синий", "MyObjectBuilder_AmmoMagazine/FireworksBoxBlue", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0007_FireworksBoxBlue", "1"}}));
                 items.Add(new Item("Fireworks Green", "Фейерверк зеленый", "MyObjectBuilder_AmmoMagazine/FireworksBoxGreen", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position00071_FireworksBoxGreen", "1"}}));
                 items.Add(new Item("Fireworks Red", "Фейерверк красный", "MyObjectBuilder_AmmoMagazine/FireworksBoxRed", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position00072_FireworksBoxRed", "1"}}));
                 items.Add(new Item("Fireworks Pink", "Фейерверк розовый", "MyObjectBuilder_AmmoMagazine/FireworksBoxPink", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position00074_FireworksBoxPink", "1"}}));
                 items.Add(new Item("Fireworks Yellow", "Фейерверк желтый", "MyObjectBuilder_AmmoMagazine/FireworksBoxYellow", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position00073_FireworksBoxYellow", "1"}}));
                 items.Add(new Item("Fireworks Rainbow", "Фейерверк радужный", "MyObjectBuilder_AmmoMagazine/FireworksBoxRainbow", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position00075_FireworksBoxRainbow", "1"}}));
                 items.Add(new Item("MR-20 Rifle Magazine", "Магазин винтовки MR-20", "MyObjectBuilder_AmmoMagazine/AutomaticRifleGun_Mag_20rd", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0040_AutomaticRifleGun_Mag_20rd", "1"}}));
                 items.Add(new Item("MR-50A Rifle Magazine", "Магазин винтовки MR-50A", "MyObjectBuilder_AmmoMagazine/RapidFireAutomaticRifleGun_Mag_50rd", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0050_RapidFireAutomaticRifleGun_Mag_50rd", "1"}}));
                 items.Add(new Item("MR-8P Rifle Magazine", "Магазин винтовки MR-8P", "MyObjectBuilder_AmmoMagazine/PreciseAutomaticRifleGun_Mag_5rd", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0060_PreciseAutomaticRifleGun_Mag_5rd", "1"}}));
                 items.Add(new Item("MR-30E Rifle Magazine", "Магазин винтовки MR-30E", "MyObjectBuilder_AmmoMagazine/UltimateAutomaticRifleGun_Mag_30rd", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0070_UltimateAutomaticRifleGun_Mag_30rd", "1"}}));
                 items.Add(new Item("5.56x45mm NATO magazine", "Магазин 5.56x45мм НАТО", "MyObjectBuilder_AmmoMagazine/NATO_5p56x45mm"));
                 items.Add(new Item("Autocannon Magazine", "Магазин автопушки", "MyObjectBuilder_AmmoMagazine/AutocannonClip", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0090_AutocannonClip", "1"}}));
                 items.Add(new Item("Gatling Ammo Box", "Боеприпасы 25x184 мм НАТО", "MyObjectBuilder_AmmoMagazine/NATO_25x184mm", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0080_NATO_25x184mmMagazine", "1"}}));
                 items.Add(new Item("Rocket", "Ракета", "MyObjectBuilder_AmmoMagazine/Missile200mm", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0100_Missile200mm", "1"}}));
                 items.Add(new Item("Artillery Shell", "Артиллерийский снаряд", "MyObjectBuilder_AmmoMagazine/LargeCalibreAmmo", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0120_LargeCalibreAmmo", "1"}}));
                 items.Add(new Item("Assault Cannon Shell", "Снаряд штурмовой пушки", "MyObjectBuilder_AmmoMagazine/MediumCalibreAmmo", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0110_MediumCalibreAmmo", "1"}}));
                 items.Add(new Item("Large Railgun Sabot", "Крупный снаряд рельсотрона", "MyObjectBuilder_AmmoMagazine/LargeRailgunAmmo", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0140_LargeRailgunAmmo", "1"}}));
                 items.Add(new Item("Small Railgun Sabot", "Малый снаряд рельсотрона", "MyObjectBuilder_AmmoMagazine/SmallRailgunAmmo", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0130_SmallRailgunAmmo", "1"}}));

                 // Others
                 items.Add(new Item("Canvas", "Полотно парашюта", "MyObjectBuilder_Component/Canvas", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0030_Canvas", "1"}}));
                 items.Add(new Item("Engineer Plushie", "Мягкая игрушка инженера", "MyObjectBuilder_Component/EngineerPlushie", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/EngineerPlushie", "1"}}));
                 items.Add(new Item("Saberoid Plushie", "Плюшевый сабероид", "MyObjectBuilder_Component/SabiroidPlushie", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/SabiroidPlushie", "1"}}));
                 items.Add(new Item("Zone Chip", "Ключ безопасности", "MyObjectBuilder_Component/ZoneChip", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/ZoneChip", "1"}}));
                 items.Add(new Item("Datapad", "Инфопланшет", "MyObjectBuilder_Datapad/Datapad", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Position0040_Datapad", "1"}}));
                 items.Add(new Item("Package", "Пакет", "MyObjectBuilder_Package/Package"));
                 items.Add(new Item("Medkit", "Аптечка", "MyObjectBuilder_ConsumableItem/Medkit"));
                 items.Add(new Item("Powerkit", "Внешний аккумулятор", "MyObjectBuilder_ConsumableItem/Powerkit"));
                 items.Add(new Item("Space Credit", "Космокредит", "MyObjectBuilder_PhysicalObject/SpaceCredit"));

                 // Paint Gun (https://steamcommunity.com/sharedfiles/filedetails/?id=500818376)
                 items.Add(new Item("Paint Chemicals", "Paint Chemicals", "MyObjectBuilder_AmmoMagazine/PaintGunMag", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Blueprint_PaintGunMag", "1"}}));
                 items.Add(new Item("Paint Gun", "Paint Gun", "MyObjectBuilder_PhysicalGunObject/PhysicalPaintGun", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Blueprint_PaintGun", "1"}}));

                 // Eat. Drink. Sleep. Repeat! (https://steamcommunity.com/sharedfiles/filedetails/?id=2547246713)
                 items.Add(new Item("Clang Kola", "Кланг-Кола", "MyObjectBuilder_ConsumableItem/ClangCola"));
                 items.Add(new Item("Cosmic Coffee", "Космокофе", "MyObjectBuilder_ConsumableItem/CosmicCoffee"));
                 items.Add(new Item("Inter-Stella Beer", "Inter-Stella Beer", "MyObjectBuilder_ConsumableItem/InterBeer"));
                 items.Add(new Item("Lies Chips", "Lies Chips", "MyObjectBuilder_ConsumableItem/LaysChips"));
                 items.Add(new Item("Single Chips", "Single Chips", "MyObjectBuilder_ConsumableItem/PrlnglesChips"));
                 items.Add(new Item("Emergency Ration", "Emergency Ration", "MyObjectBuilder_ConsumableItem/Emergency_Ration", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Emergency_Ration", "1"}}));

                 // Plant and Cook (https://steamcommunity.com/sharedfiles/filedetails/?id=2570427696)
                 // Plant and Cook (Crowigor's Edition) (https://steamcommunity.com/sharedfiles/filedetails/?id=3243597879)
                 items.Add(new Item("Organic", "Органика", "MyObjectBuilder_Ore/Organic", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/CompostingAlgae", "9"}, {"MyObjectBuilder_BlueprintDefinition/CompostingApple", "4"}, {"MyObjectBuilder_BlueprintDefinition/CompostingCabbage", "6"}, {"MyObjectBuilder_BlueprintDefinition/CompostingH2Algae", "9"}, {"MyObjectBuilder_BlueprintDefinition/CompostingHerbs", "2"}, {"MyObjectBuilder_BlueprintDefinition/CompostingMushrooms", "4"}, {"MyObjectBuilder_BlueprintDefinition/CompostingPumpkin", "8"}, {"MyObjectBuilder_BlueprintDefinition/CompostingSoya", "4"}, {"MyObjectBuilder_BlueprintDefinition/CompostingWheat", "4"}}));
                 items.Add(new Item("Sparkling Water", "Газированная Вода", "MyObjectBuilder_ConsumableItem/SparklingWater", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/SparklingWater", "1"}, {"MyObjectBuilder_BlueprintDefinition/SparklingWaterCan", "1"}}));
                 items.Add(new Item("Empty Tin Can", "Пустая Жестяная Банка", "MyObjectBuilder_Component/EmptyTinCan", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/EmptyTinCan", "1"}}));
                 items.Add(new Item("Europa Ice Tea", "Ледяной чай «Европа»", "MyObjectBuilder_ConsumableItem/EuropaTea", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/EuropaTea", "1"}}));
                 items.Add(new Item("Apple", "Яблоко", "MyObjectBuilder_ConsumableItem/Apple", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/FarmedApples", "20"}}));
                 items.Add(new Item("Apple Juice", "Яблочный Сок", "MyObjectBuilder_ConsumableItem/AppleJuice", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/AppleJuice", "1"}}));
                 items.Add(new Item("Apple Pie", "Яблочный Пирог", "MyObjectBuilder_ConsumableItem/ApplePie", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/ApplePie", "1"}}));
                 items.Add(new Item("Wheat", "Пшеница", "MyObjectBuilder_Ingot/Wheat", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/FarmedWheat", "40"}}));
                 items.Add(new Item("Pumpkin", "Тыква", "MyObjectBuilder_Ingot/Pumpkin", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/FarmedPumpkin", "10"}}));
                 items.Add(new Item("Cabbage", "Капуста", "MyObjectBuilder_Ingot/Cabbage", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/FarmedCabbage", "12"}}));
                 items.Add(new Item("Soya Beans", "Соевые Бобы", "MyObjectBuilder_Ingot/Soya", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/FarmedSoya", "30"}, {"MyObjectBuilder_BlueprintDefinition/Soya", "8"}}));
                 items.Add(new Item("Herbs", "Травы", "MyObjectBuilder_Ingot/Herbs", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/FarmedHerbs", "20"}, {"MyObjectBuilder_BlueprintDefinition/Herbs", "5"}}));
                 items.Add(new Item("Bread", "Хлеб", "MyObjectBuilder_ConsumableItem/Bread", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Bread", "1"}}));
                 items.Add(new Item("Burger", "Бургер", "MyObjectBuilder_ConsumableItem/Burger", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Burger", "1"}}));
                 items.Add(new Item("Meat", "Мясо", "MyObjectBuilder_ConsumableItem/Meat"));
                 items.Add(new Item("Roast Meat", "Жареное Мясо", "MyObjectBuilder_ConsumableItem/MeatRoasted", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/MeatRoasted", "1"}}));
                 items.Add(new Item("Meat Soup", "Мясной Суп", "MyObjectBuilder_ConsumableItem/Soup", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Soup", "1"}}));
                 items.Add(new Item("Mushroom Soup", "Грибной Суп", "MyObjectBuilder_ConsumableItem/MushroomSoup", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/MushroomSoup", "1"}}));
                 items.Add(new Item("Tofu Soup", "Суп Из Тофу", "MyObjectBuilder_ConsumableItem/TofuSoup", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/TofuSoup", "1"}}));
                 items.Add(new Item("Tofu", "Тофу", "MyObjectBuilder_ConsumableItem/Tofu", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/Tofu", "1"}}));
                 items.Add(new Item("Mushrooms", "Грибы", "MyObjectBuilder_ConsumableItem/Mushrooms", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/FarmedMushrooms", "20"}}));
                 items.Add(new Item("Steak with Mushrooms", "Стейк С Грибами", "MyObjectBuilder_ConsumableItem/ShroomSteak", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/ShroomSteak", "1"}}));

                 // AiEnabled (https://steamcommunity.com/sharedfiles/filedetails/?id=2596208372)
                 items.Add(new Item("Combat Bot Material", "Combat Bot Material", "MyObjectBuilder_Component/AiEnabled_Comp_CombatBotMaterial", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/AiEnabled_BP_CombatBotMaterial", "1"}}));
                 items.Add(new Item("Crew Bot Material", "Crew Bot Material", "MyObjectBuilder_Component/AiEnabled_Comp_CrewBotMaterial", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/AiEnabled_BP_CrewBotMaterial", "1"}}));
                 items.Add(new Item("Repair Bot Material", "Repair Bot Material", "MyObjectBuilder_Component/AiEnabled_Comp_RepairBotMaterial", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/AiEnabled_BP_RepairBotMaterial", "1"}}));
                 items.Add(new Item("Scavenger Bot Material", "Scavenger Bot Material", "MyObjectBuilder_Component/AiEnabled_Comp_ScavengerBotMaterial", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/AiEnabled_BP_ScavengerBotMaterial", "1"}}));

                 // Personal Shield Generators (https://steamcommunity.com/sharedfiles/filedetails/?id=1330335279)
                 items.Add(new Item("Personal Shield Generator", "Personal Shield Generator", "MyObjectBuilder_PhysicalObject/EngineerShield", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/PersonalShieldItem", "1"}}));

                 // Defense Shields (https://steamcommunity.com/sharedfiles/filedetails/?id=3154379105)
                 items.Add(new Item("Field Emitter", "Field Emitter", "MyObjectBuilder_Component/ShieldComponent", new Dictionary<string, string>() {{"MyObjectBuilder_BlueprintDefinition/ShieldComponentBP", "1"}}));

                return items;
            }
        }
    }
}