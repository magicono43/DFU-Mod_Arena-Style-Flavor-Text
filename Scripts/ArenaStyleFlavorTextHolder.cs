using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop;
using DaggerfallConnect.Arena2;
using DaggerfallConnect;
using DaggerfallWorkshop.Game.Player;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Utility;

namespace ArenaStyleFlavorText
{
    public partial class ArenaStyleFlavorTextMain
    {
        public static string BuildName()
        {
            return GameManager.Instance.PlayerEnterExit.BuildingDiscoveryData.displayName;
        }

        public static string CityName()
        {   // %cn
            PlayerGPS gps = GameManager.Instance.PlayerGPS;
            if (gps.HasCurrentLocation)
                return gps.CurrentLocation.Name;
            else
                return gps.CurrentRegion.Name;
        }

        private static string CurrentRegion()
        {   // %crn going to use for %reg as well here
            return GameManager.Instance.PlayerGPS.CurrentRegion.Name;
        }

        public static string RegentName()
        {   // %rn
            // Look for a defined ruler for the region.
            PlayerGPS gps = GameManager.Instance.PlayerGPS;
            PersistentFactionData factionData = GameManager.Instance.PlayerEntity.FactionData;
            FactionFile.FactionData regionFaction;
            if (factionData.FindFactionByTypeAndRegion((int)FactionFile.FactionTypes.Province, gps.CurrentRegionIndex, out regionFaction))
            {
                FactionFile.FactionData child;
                foreach (int childID in regionFaction.children)
                    if (factionData.GetFactionData(childID, out child) && child.type == (int)FactionFile.FactionTypes.Individual)
                        return child.name;
            }
            // Use a random name if no defined individual ruler.
            return GetRandomFullName();
        }

        public static string GetRandomFullName()
        {
            // Get appropriate nameBankType for this region and a random gender
            NameHelper.BankTypes nameBankType = NameHelper.BankTypes.Breton;
            if (GameManager.Instance.PlayerGPS.CurrentRegionIndex > -1)
                nameBankType = (NameHelper.BankTypes)MapsFile.RegionRaces[GameManager.Instance.PlayerGPS.CurrentRegionIndex];
            Genders gender = (DFRandom.random_range_inclusive(0, 1) == 1) ? Genders.Female : Genders.Male;

            return DaggerfallUnity.Instance.NameHelper.FullName(nameBankType, gender);
        }

        public static string RegentTitle()
        {   // %rt %t
            PlayerGPS gps = GameManager.Instance.PlayerGPS;
            FactionFile.FactionData regionFaction;
            GameManager.Instance.PlayerEntity.FactionData.FindFactionByTypeAndRegion((int)FactionFile.FactionTypes.Province, gps.CurrentRegionIndex, out regionFaction);
            return GetRulerTitle(regionFaction.ruler);
        }

        private static string GetRulerTitle(int factionRuler)
        {
            switch (factionRuler)
            {
                case 1:
                    return TextManager.Instance.GetLocalizedText("King");
                case 2:
                    return TextManager.Instance.GetLocalizedText("Queen");
                case 3:
                    return TextManager.Instance.GetLocalizedText("Duke");
                case 4:
                    return TextManager.Instance.GetLocalizedText("Duchess");
                case 5:
                    return TextManager.Instance.GetLocalizedText("Marquis");
                case 6:
                    return TextManager.Instance.GetLocalizedText("Marquise");
                case 7:
                    return TextManager.Instance.GetLocalizedText("Count");
                case 8:
                    return TextManager.Instance.GetLocalizedText("Countess");
                case 9:
                    return TextManager.Instance.GetLocalizedText("Baron");
                case 10:
                    return TextManager.Instance.GetLocalizedText("Baroness");
                case 11:
                    return TextManager.Instance.GetLocalizedText("Lord");
                case 12:
                    return TextManager.Instance.GetLocalizedText("Lady");
                default:
                    return TextManager.Instance.GetLocalizedText("Lord");
            }
        }

        public static string RemoteTown()
        {   // Replaces __City__
            int maxAttemptsBeforeFailure = 500;

            // Get player region
            int regionIndex = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
            DFRegion regionData = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetRegion(regionIndex);
            int playerLocationIndex = GameManager.Instance.PlayerGPS.CurrentLocationIndex;

            // Cannot use a region with no locations
            // This should not happen in normal play
            if (regionData.LocationCount == 0)
                return "Wutville";

            int attempts = 0;
            bool found = false;
            while (!found)
            {
                // Increment attempts and do some fallback
                if (++attempts >= maxAttemptsBeforeFailure)
                    break;

                // Get a random location index
                int locationIndex = UnityEngine.Random.Range(0, (int)regionData.LocationCount);

                // Discard the current player location if selected
                if (locationIndex == playerLocationIndex)
                    continue;

                // Discard all dungeon location types
                if (IsDungeonType(regionData.MapTable[locationIndex].LocationType))
                    continue;

                // Only allow certain location types, in this case cities and settlements, etc.
                if (!IsTownType(regionData.MapTable[locationIndex].LocationType))
                    continue;

                // Get location data for town
                DFLocation location = DaggerfallUnity.Instance.ContentReader.MapFileReader.GetLocation(regionIndex, locationIndex);

                return location.Name;
            }

            DFRegion dfRegion = GameManager.Instance.PlayerGPS.CurrentRegion;
            for (int i = 0; i < dfRegion.LocationCount; i++)
            {
                if (GameManager.Instance.PlayerGPS.CurrentLocationIndex != i && dfRegion.MapTable[i].LocationType == DFRegion.LocationTypes.TownCity)
                    return dfRegion.MapNames[i];
            }
            return GameManager.Instance.PlayerGPS.CurrentRegion.Name;
        }

        /// <summary>
        /// Checks if location is one of the dungeon types.
        /// </summary>
        public static bool IsDungeonType(DFRegion.LocationTypes locationType)
        {
            // Consider 3 major dungeon types and 2 graveyard types as dungeons
            // Will exclude locations with dungeons, such as Daggerfall, Wayrest, Sentinel
            if (locationType == DFRegion.LocationTypes.DungeonKeep ||
                locationType == DFRegion.LocationTypes.DungeonLabyrinth ||
                locationType == DFRegion.LocationTypes.DungeonRuin ||
                locationType == DFRegion.LocationTypes.Graveyard)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if location is one of the valid town types.
        /// </summary>
        public static bool IsTownType(DFRegion.LocationTypes locationType)
        {
            if (locationType == DFRegion.LocationTypes.TownCity ||
                locationType == DFRegion.LocationTypes.TownHamlet ||
                locationType == DFRegion.LocationTypes.TownVillage)
            {
                return true;
            }
            return false;
        }

        public static string RandomAlcohol() // Might want to add more "fantasy" sounding drinks to this at some point, but for now it should be alright hopefully.
        {
            DFLocation.ClimateBaseType climate = GameManager.Instance.PlayerGPS.ClimateSettings.ClimateType;
            DaggerfallDateTime.Seasons season = DaggerfallUnity.Instance.WorldTime.Now.SeasonValue;

            int variant = UnityEngine.Random.Range(0, 2);

            switch (climate)
            {
                case DFLocation.ClimateBaseType.Desert:
                    switch (season)
                    {
                        case DaggerfallDateTime.Seasons.Fall:
                            return variant == 0 ? "Jalapeno tequila" : "Prickly pear margarita";
                        case DaggerfallDateTime.Seasons.Spring:
                        default:
                            return variant == 0 ? "Coconut rum" : "Cucumber mojito";
                        case DaggerfallDateTime.Seasons.Summer:
                            return variant == 0 ? "Pineapple margarita" : "Cucumber mojito";
                        case DaggerfallDateTime.Seasons.Winter:
                            return variant == 0 ? "Cinnamon tequila" : "Spicy scorpion cocktail";
                    }
                case DFLocation.ClimateBaseType.Mountain:
                    switch (season)
                    {
                        case DaggerfallDateTime.Seasons.Fall:
                            return variant == 0 ? "Mulled cider" : "Cinnamon mead";
                        case DaggerfallDateTime.Seasons.Spring:
                        default:
                            return variant == 0 ? "Elderberry cocktail" : "Juniper berry mead";
                        case DaggerfallDateTime.Seasons.Summer:
                            return variant == 0 ? "Barley wine" : "Golden ale";
                        case DaggerfallDateTime.Seasons.Winter:
                            return variant == 0 ? "Mulled wine" : "Wassail";
                    }
                case DFLocation.ClimateBaseType.Temperate:
                default:
                    switch (season)
                    {
                        case DaggerfallDateTime.Seasons.Fall:
                            return variant == 0 ? "Cranberry wine" : "Pumpkin sangria";
                        case DaggerfallDateTime.Seasons.Spring:
                        default:
                            return variant == 0 ? "Watermelon Mojito" : "Tequila sunrise";
                        case DaggerfallDateTime.Seasons.Summer:
                            return variant == 0 ? "Mint julep" : "Elderflower champagne";
                        case DaggerfallDateTime.Seasons.Winter:
                            return variant == 0 ? "Mulled wine" : "Anise Liqueur";
                    }
                case DFLocation.ClimateBaseType.Swamp:
                    switch (season)
                    {
                        case DaggerfallDateTime.Seasons.Fall:
                            return variant == 0 ? "Banana liqueur" : "Grapefruit mimosa";
                        case DaggerfallDateTime.Seasons.Spring:
                        default:
                            return variant == 0 ? "Sake sour" : "Grapefruit daiquiri";
                        case DaggerfallDateTime.Seasons.Summer:
                            return variant == 0 ? "Pineapple sangria" : "Sake mojito";
                        case DaggerfallDateTime.Seasons.Winter:
                            return variant == 0 ? "Snakebite cocktail" : "Coconut rum";
                    }
            }
        }


        #region Shop Text

        public static TextFile.Token[] HotFallShopText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ". Many items of interest sit on display shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = "It is a miserable autumn day outside, and it is good to get inside " + BuildName() + ". You glance over the new shipments of supplies and wares.";
                else
                    raw = "It is a bit early in the year for this sort of slightly colder weather, but new supplies arrived at " + BuildName() + " with the chill. You find a few useful items immediately...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", happy to be out of the warm shower. As you dry off you notice the many items of interest that sit on the display shelves around the shop...";
                else if (variant == 1)
                    raw = "You are handed a towel as you come into " + BuildName() + " from the autumn thunderstorm. There is a new shipment of wares and supplies, and you notice several pieces worth looking at...";
                else
                    raw = "You are dripping with warm rain water as you enter " + BuildName() + ". It is a neat and clean chamber with a wide assortment of supplies and wares from this shop's speciality...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "As you enter " + BuildName() + ", golden glints from the autumn sun reflect off of the many items of interest scattered about...";
                else if (variant == 1)
                    raw = "You enter " + BuildName() + " from the sunny autumn day. Display shelves of this store's speciality are featured next to some of the more peaceful supplies and gear...";
                else
                    raw = "The pleasant autumn weather has given " + BuildName() + " an air of joviality. You browse through cases and displays of various supplies...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] HotSpringShopText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0) // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You enter " + BuildName() + ", silently thanking the dense clouds for shielding you from the dreaded sun today. You notice several items of interest on display...";
                    else
                        raw = "You enter " + BuildName() + ", hoping that the sun will show itself again. There are many items of interest displayed on the shelves around the shopkeeper...";
                }
                else if (variant == 1)
                    raw = "There is a window open in " + BuildName() + ": the shopkeeper is apparently hoping for some fresh scents of spring to brighten a gray day. Several wares and supplies on display might be helpful to you...";
                else
                    raw = "It is a cold and overcast day, and you doubt that the shopkeeper in " + BuildName() + " is in the mood to barter. You check your money supply as you browse through the store's wares...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", happy to be out of the warm spring shower. Many items of interest are displayed on the shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = "It looks like a new shipment of supplies arrived in " + BuildName() + "at the same time the rain picked up outside. You notice several items of interest...";
                else
                    raw = "You wipe the refreshing spring rain from your shoulders and head as you enter " + BuildName() + ". Most of the items within are meant more for the townsmen than you, but you do see some adventuring equipment...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " and bring with you a cool spring breeze that ruffles many of the items of interest displayed on the shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = "It is a beautiful spring day outside, and the mood of the shopkeeper in " + BuildName() + " is bright. Perhaps you can get a good deal off him on a couple of items that impress you...";
                else
                    raw = "An open window in " + BuildName() + " brings in the smell of spring flowers as you look over your money to see what you can afford. There are several displayed items that might prove useful...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] HotSummerShopText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into " + BuildName() + ". Many items of interest are displayed on the shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = BuildName() + " is a refuge from the steamy summer day. On the shelves are supplies and wares for sale...";
                else
                    raw = "You enter " + BuildName() + " from the overcast summer day. You browse over the items displayed throughout the store and are impressed by the variety...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", happy to be out of the summer rain. The cool shade causes shivers to run over your damp body. Many items of interest sit on display shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = "The interior of " + BuildName() + " is spotlessly clean and neat. You feel almost embarrassed to be dripping puddles of warm rain water all over as you glance over the wares displayed on the shelves...";
                else
                    raw = "Wiping the warm water from the summer shower off your head, you enter " + BuildName() + ". A display case of the more popular supplies is featured in the small room.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", happy to be out of the summer heat. Many items of interest are displayed on the shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = "The interior of " + BuildName() + " is neat and well organized. You wipe the sweat from your brow and look over the various wares and supplies...";
                else
                    raw = "It is a relief to enter " + BuildName() + ", out of the infernal summer sunshine. Various supplies are carefully arranged throughout the store for your browsing convenience...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] HotWinterShopText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0) // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You enter " + BuildName() + ", silently thanking the dense clouds for shielding you from the dreaded sun today. You notice several items of interest on display...";
                    else
                        raw = "You enter " + BuildName() + ", hoping that the sun will show itself again. There are many items of interest displayed on the shelves around the shopkeeper...";
                }
                else if (variant == 1)
                    raw = "The pleasant weather has given " + BuildName() + " an air of joviality. You browse through cases and and displays of various supplies...";
                else
                    raw = "It is a cold and overcast day, and you doubt that the shopkeeper in " + BuildName() + " is in the mood to barter. You check your money supply as you browse through the store's wares...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", happy to be out of the chill shower. As you dry off you notice the many items of interest that sit on the display shelves around the shop...";
                else if (variant == 1)
                    raw = "You are handed a towel as you come into " + BuildName() + " from the thunderstorm. There is a new shipment of wares and supplies, and you notice several pieces worth looking at...";
                else
                    raw = "You are dripping with cold rain water as you enter " + BuildName() + ". It is a neat and clean chamber with a wide assortment of supplies and wares from this shop's speciality...";
            }
            else if (weatherID == 3) // Snowy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", stamping your feet to warm them and shaking the frozen snow from your shoulders. Many items of interest are displayed on shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = "A warm fire in " + BuildName() + " thaws your frozen body and the snow on your shoulders and head quickly melts. You notice several supplies on the shelves that just might be useful...";
                else
                    raw = "You let a spray of snow and wind into " + BuildName() + " as you enter. Still, with the exception of the puddle by the door, the chamber is neat and tidy. The shelves are fully stocked with all varieties of wares...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", happy to be out of the chilly and bright day. Many items of interest are displayed on the shelves in the main room...";
                else if (variant == 1)
                    raw = BuildName() + " is as bright as the day outside and much warmer. Several wares and equipment look interesting to you...";
                else // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You walk into " + BuildName() + ". A short refuge from the agonizing sun this dreadfully clear day. You absentmindedly rub the fresh blisters on your skin, as you browse the various items on the store shelves.";
                    else
                        raw = "You walk into " + BuildName() + ". At least it is sunny outside. Many new wares and supplies impress you enough for a closer look...";
                }
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainFallShopText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " rubbing your hands together to warm them from the chill. Many items of interest sit on display shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = "It is a miserable autumn day outside, and it is good to get inside " + BuildName() + ". You glance over the new shipments of supplies and wares.";
                else
                    raw = "It is a bit strange this time of year to get warmer weather, but new supplies arrived at " + BuildName() + " with the cold. You find a few useful items immediately...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", happy to be out of the cold shower. As you dry off you notice the many items of interest that sit on the display shelves around the shop...";
                else if (variant == 1)
                    raw = "You are handed a towel as you come into " + BuildName() + " from the autumn thunderstorm. There is a new shipment of wares and supplies, and you notice several pieces worth looking at...";
                else
                    raw = "You are dripping with cold rain water as you enter " + BuildName() + ". It is a neat and clean chamber with a wide assortment of supplies and wares from this shop's speciality...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "As you enter " + BuildName() + ", golden glints from the fall sun reflect off of the many items of interest scattered about...";
                else if (variant == 1)
                    raw = "You enter " + BuildName() + " from the sunny autumn day. Display shelves of this store's speciality are featured next to some of the more peaceful supplies and gear...";
                else
                    raw = "The strangely pleasant autumn weather has given " + BuildName() + " an air of joviality. You browse through cases and displays of various supplies...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainSpringShopText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0) // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You enter " + BuildName() + ", silently thanking the dense clouds for shielding you from the dreaded sun today. You notice several items of interest on display...";
                    else
                        raw = "You enter " + BuildName() + ", hoping that the sun will show itself again. There are many items of interest displayed on the shelves around the shopkeeper...";
                }
                else if (variant == 1)
                    raw = "There is a window open in " + BuildName() + ": the shopkeeper is apparently hoping for some fresh scents of spring air to brighten a gray day. Several wares and supplies on display might be helpful to you...";
                else
                    raw = "It is a cold and overcast day, and you doubt that the shopkeeper in " + BuildName() + " is in the mood to barter. You check your money supply as you browse through the store's wares...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", happy to be out of the cold spring shower. Many items of interest are displayed on the shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = "It looks like a new shipment of supplies arrived in " + BuildName() + " at the same time the rain picked up outside. You notice several items of interest...";
                else
                    raw = "You wipe the cool spring rain from your shoulders and head as you enter " + BuildName() + ". Most of the items within are meant more for the townsmen than you, but you do see some adventuring equipment...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " and bring with you a chill spring breeze that ruffles many of the items of interest displayed on the shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = "It is a beautiful spring day outside, and the mood of the shopkeeper in " + BuildName() + " is bright. Perhaps you can get a good deal off him on a couple of items that impress you...";
                else
                    raw = "An open window in " + BuildName() + " brings in the smell of spring flowers as you look over your money to see what you can afford. There are several displayed items that might prove useful...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainSummerShopText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into " + BuildName() + ". Many items of interest are displayed on the shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = BuildName() + " is a refuge from the windy summer day. On the shelves are supplies and wares for sale...";
                else
                    raw = "You enter " + BuildName() + " from the overcast summer day. You browse over the items displayed throughout the store and are impressed by the variety...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", happy to be out of the summer rain. The cool shade causes shivers to run over your damp body. Many items of interest sit on display shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = "The interior of " + BuildName() + " is spotlessly clean and neat. You feel almost embarrassed to be dripping puddles of cold rain water water all over as you glance over the wares displayed on the shelves...";
                else
                    raw = "Wiping the cold water from the summer shower off your head, you enter " + BuildName() + ". A display case of the more popular supplies is featured in the small room.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", happy to be out of the summer glare. Many items of interest are displayed on the shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = "The interior of " + BuildName() + " is neat and well organized. You wipe the sweat from your brow and look over the various wares and supplies...";
                else
                    raw = "It is a relief to enter " + BuildName() + ", out of the chill wind and blinding summer sunshine. Various supplies are carefully arranged throughout the store for your browsing convenience...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainWinterShopText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ". Outside the sun is hidden behind a thick gray layer of clouds, dropping the temperature and making your body steam in the warmth of the shopkeeper's hearth. Many items of are displayed on shelves in the main room...";
                else if (variant == 1)
                    raw = "You are quickly given a cup of warm cider as you enter " + BuildName() + ", which you accept gratefully, happy to be inside on such a gloomy winter's day. You look over the wares and other merchandise proudly displayed on shelves around the shopkeeper.";
                else
                    raw = "The dog days of winter are certainly here. You enter " + BuildName() + ", hoping for a deal or two on a couple essential wares. Several things immediately attract your attention as you look over the shelves and the assortments around the shopkeeper....";
            }
            else if (weatherID == 3) // Snowy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", stamping your feet to warm them and shaking the frozen snow from your shoulders. Many items of interest are displayed on shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = "A warm fire in " + BuildName() + " thaws your frozen body and the snow on your shoulders and head quickly melts. You notice several supplies on the shelves that just might be useful...";
                else
                    raw = "You let a spray of snow and wind into " + BuildName() + " as you enter. Still, with the exception of the puddle by the door, the chamber is neat and tidy. The shelves are fully stocked with all varieties of wares...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", happy to be out of the cold and bright winter's day. Many items of interest are displayed on the shelves in the main room...";
                else if (variant == 1)
                    raw = BuildName() + " is as bright as the winter day outside and much colder. Several wares and equipment look interesting to you...";
                else // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You walk into " + BuildName() + ". A short refuge from the agonizing sun this dreadfully clear day. You absentmindedly rub the fresh blisters on your skin, as you browse the various items on the store shelves.";
                    else
                        raw = "You walk into " + BuildName() + ", rubbing your numb hands together. At least it is sunny outside. Many new wares and supplies impress you enough for a closer look...";
                }
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateFallShopText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " rubbing your hands together to warm them from the chill. Many items of interest sit on display shelves around the shop keeper...";
                else if (variant == 1)
                    raw = "It is a miserable autumn day outside, and it is good to get inside " + BuildName() + ". You glance over the new shipments of supplies and wares.";
                else
                    raw = "It is a bit early in the year for this sort of chilling weather, but new supplies arrived at " + BuildName() + " with the cold. You find a few useful items immediately...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", happy to be out of the cold shower. As you dry off you notice the many items of interest that sit on the display shelves around the shop...";
                else if (variant == 1)
                    raw = "You are handed a towel as you come into " + BuildName() + " from the autumn thunderstorm. There is a new shipment of wares and supplies, and you notice several pieces worth looking at...";
                else
                    raw = "You are dripping with cold rain water as you enter " + BuildName() + ". It is a neat and clean chamber with a wide assortment of supplies and wares from this shop's speciality...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "As you enter " + BuildName() + ", golden glints from the autumn sun reflect off of the many items of interest scattered about...";
                else if (variant == 1)
                    raw = "You enter " + BuildName() + " from the sunny autumn day. Display shelves of this store's speciality are featured next to some of the more peaceful supplies and gear...";
                else
                    raw = "The pleasant autumn weather has given " + BuildName() + " an air of joviality. You browse through cases and displays of various supplies...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateSpringShopText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", hoping that the sun will show itself again. There are many items of interest displayed on the shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = "There is a window open in " + BuildName() + ": the shopkeeper is apparently hoping for some fresh scents of spring to brighten a gray day. Several wares and supplies on display might be helpful to you...";
                else
                    raw = "It is a cold and overcast day, and you doubt that the shopkeeper in " + BuildName() + " is in the mood to barter. You check your money supply as you browse through the store's wares...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", happy to be out of the cool spring shower. Many items of interest are displayed on the shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = "It looks like a new shipment of supplies arrived in " + BuildName() + " at the same time the rain picked up outside. You notice several items of interest...";
                else
                    raw = "You wipe the cool spring rain from your shoulders and head as you enter " + BuildName() + ". Most of the items within are meant more for the townsmen than you, but you do see some adventuring equipment...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " and bring with you a cool spring breeze that ruffles many of the items of interest displayed on the shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = "It is a beautiful spring day outside, and the mood of the shopkeeper in " + BuildName() + " is bright. Perhaps you can get a good deal off him on a couple of items that impress you...";
                else
                    raw = "An open window in " + BuildName() + " brings in the smell of spring flowers as you look over your money to see what you can afford. There are several displayed items that might prove useful...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateSummerShopText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into " + BuildName() + ". Many items of interest are displayed on the shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = BuildName() + " is a refuge from the steamy summer day. On the shelves are supplies and wares for sale...";
                else
                    raw = "You enter " + BuildName() + " from the overcast summer day. You browse over the items displayed throughout the store and are impressed by the variety...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", happy to be out of the summer rain. The cool shade causes shivers to run over your damp body. Many items of interest sit on display shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = "The interior of " + BuildName() + " is spotlessly clean and neat. You feel almost embarrassed to be dripping puddles of warm rain water water all over as you glance over the wares displayed on the shelves...";
                else
                    raw = "Wiping the warm water from the summer shower off your head, you enter " + BuildName() + ". A display case of the more popular supplies is featured in the small room.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", happy to be out of the summer heat. Many items of interest are displayed on the shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = "The interior of " + BuildName() + " is neat and well organized. You wipe the sweat from your brow and look over the various wares and supplies...";
                else
                    raw = "It is a relief to enter " + BuildName() + ", out of the infernal summer sunshine. Various supplies are carefully arranged throughout the store for your browsing convenience...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateWinterShopText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ". Outside the sun is hidden behind a thick gray layer of clouds, dropping the temperature and making your body steam in the warmth of the shopkeeper's hearth. Many items of are displayed on shelves in the main room...";
                else if (variant == 1)
                    raw = "You are quickly given a cup of hot cider as you enter " + BuildName() + ", which you accept gratefully, happy to be inside on such a gloomy winter's day. You look over the wares and other merchandise proudly displayed on shelves around the shopkeeper.";
                else
                    raw = "The dog days of winter are certainly here. You enter " + BuildName() + ", hoping for a deal or two on a couple essential wares. Several things immediately attract your attention as you look over the shelves and the assortments around the shopkeeper...";
            }
            else if (weatherID == 3) // Snowy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", stamping your feet to warm them and shaking the frozen snow from your shoulders. Many items of interest are displayed on shelves around the shopkeeper...";
                else if (variant == 1)
                    raw = "A warm fire in " + BuildName() + " thaws your frozen body and the snow on your shoulders and head quickly melts. You notice several supplies on the shelves that just might be useful...";
                else
                    raw = "You let a spray of snow and wind into " + BuildName() + " as you enter. Still, with the exception of the puddle by the door, the chamber is neat and tidy. The shelves are fully stocked with all varieties of wares...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", happy to be out of the cold and bright winter's day. Many items of interest are displayed on the shelves in the main room...";
                else if (variant == 1)
                    raw = BuildName() + " is as bright as the winter day outside and much warmer. Several wares and equipment look interesting to you...";
                else // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You walk into " + BuildName() + ". A short refuge from the agonizing sun this dreadfully clear day. You absentmindedly rub the fresh blisters on your skin, as you browse the various items on the store shelves.";
                    else
                        raw = "You walk into " + BuildName() + ", rubbing your numb hands together. At least it is sunny outside. Many new wares and supplies impress you enough for a closer look...";
                }
            }
            return TextTokenFromRawString(raw);
        }

        #endregion

        #region Tavern Text

        public static TextFile.Token[] HotFallTavernText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (DaggerfallUnity.Instance.WorldTime.Now.IsNight)
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", rubbing the chill night air from your bones. The tavern is full of people bustling here and there...";
                else if (variant == 1)
                    raw = "On this cold autumn night, " + BuildName() + " seems especially inviting. You smell apples baking in the kitchen and look forward to your first serving of " + RandomAlcohol() + "...";
                else
                    raw = "The warm smells of tobacco smoke and simple foods draw you into " + BuildName() + " from the cold autumn night...";
            }

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "The tavern you enter is brightly lit and colorful, compared to the cold, dark clouds that hang in the sky...";
                else if (variant == 1)
                    raw = "You walk out of the cloudy autumn day into " + BuildName() + ", the smell of baking bread pulling on you like a lure...";
                else
                    raw = "On an overcast autumn day like today, " + BuildName() + " is very popular and you can see why. It certainly lifts the spirits...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " and shake the warm rain from your shoulders, hoping for a fire of some sort to dry yourself...";
                else if (variant == 1)
                    raw = "Warm winds and rain follow you into " + BuildName() + " like a beggar. You wipe your wet shoulders and head as you head for the fireplace...";
                else
                    raw = "Wet from the warm autumn tempest, you escape into " + BuildName() + ". Other miserably wet people have gathered in dripping groups...";
            }
            else // Sunny or anything else
            {
                if (variant == 0) // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You enter " + BuildName() + ", heartened by the smells of food and spices, and prepare to relax in a nice dark corner away from the cruel sun...";
                    else
                        raw = "You enter " + BuildName() + ", heartened by the smells of food and spices, and prepare to relax in the warm sunlight...";
                }
                else if (variant == 1)
                    raw = "The fiery autumn sun disappears as you enter " + BuildName() + ". The sounds of laughter, clinking glasses, and banging cooking pans meet your ears...";
                else
                    raw = "The townsmen have met in " + BuildName() + " to celebrate a good fall harvest. Their exuberance is infectious. Oh great, you say to yourself, I'll probably have this stupid drinking song in my head all day...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] HotSpringTavernText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (DaggerfallUnity.Instance.WorldTime.Now.IsNight)
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", searching for a clear table at which you can relax on this cool spring night...";
                else if (variant == 1)
                    raw = BuildName() + " is like a beacon in the cool spring night, full of torch light, clinking glasses, and the aromas of the kitchen...";
                else
                    raw = "It is a cool spring night outside, but you have no need to seek out the fireplace on entering " + BuildName() + ". The tobacco smoke and smell of new bread trickles out the open windows and into the night...";
            }

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = BuildName() + " colorful interior adds life to the otherwise gray spring day...";
                else if (variant == 1)
                    raw = "Despite the rather cloudy conditions outside, the atmosphere inside " + BuildName() + " is extremely lively. Someone could even get hurt...";
                else
                    raw = "You leave the overcast spring day and enter " + BuildName() + ".The smell of simple baked breads coming from the kitchen seems like an invitation to stay.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " and wipe your rain soaked head on a small towel offered by the serving wench. Outside the rare spring shower continues...";
                else if (variant == 1)
                    raw = "You are dripping wet as you enter " + BuildName() + ", but then again, so are most of the rest of the patrons. The barwench is busily passing out towels as well as drinks...";
                else
                    raw = "You can still hear the spring tempest outside, beating against the walls of " + BuildName() + ". One of the maids stands by the door, attempting to mop up a persistent leak...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "A cool breeze follows you into " + BuildName() + ", bringing with it the smell of newly bloomed flowers and fresh air...";
                else if (variant == 1)
                    raw = "Through the open door and windows, all the smells of spring come into " + BuildName() + " and the chill of the winter months fly out...";
                else // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You walk into " + BuildName() + ", thankful to have at least a moment of reprieve from the horribly sunny spring day...";
                    else
                        raw = "You walk into " + BuildName() + ", immediately wishing you were again out in the pleasant spring day...";
                }
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] HotSummerTavernText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (DaggerfallUnity.Instance.WorldTime.Now.IsNight)
            {
                if (variant == 0)
                    raw = "You are greeted by warm laughter and merry talk as you enter " + BuildName() + ". Torches around the tavern shed light on this cold summer night...";
                else if (variant == 1)
                    raw = "The heat and humidity of the day have disappeared, and it has turned out to be a rather cold night. The torches on the walls of " + BuildName() + " light up many a sunburned face...";
                else
                    raw = "The summer night is colder as the day had been, and you enter " + BuildName() + " craving something warm and dry...";
            }

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "Although the sun isn't shining, the humidity has plastered your clothes to your back like a second skin. As you close the door to " + BuildName() + " your eyes search for a cool dark corner in which to rest...";
                else if (variant == 1)
                    raw = "It is uncomfortably hot and sticky in " + BuildName() + ", almost as bad as the cloudy summer day outside...";
                else
                    raw = "You walk into " + BuildName() + " from the hot, cloudy day, welcomed by the other patrons sequestered away from the elements...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You shake the rain from your shoulders and enter " + BuildName() + ", hoping for a place where you can at last rest and dry out...";
                else if (variant == 1)
                    raw = "A burst of the warm summer rain follows you into " + BuildName() + ", and you follow the many paths of muddy footprints to the bar...";
                else
                    raw = "Dripping from the summer tempest outside, you enter " + BuildName() + ", relieved to have found shelter...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "Cool shade and cooler drinks greet you as you make your way out of the heat and into " + BuildName() + ", smiling at the serving wench who holds a glass of " + RandomAlcohol() + " ready for the first thirsty patron...";
                else if (variant == 1)
                    raw = "It is a relief to move inside " + BuildName() + ", away from the burning summer heat, and you feel that a cold mug of grog might be in order...";
                else
                    raw = "You enter " + BuildName() + ", feeling the cool shadows fall over your face like a blanket...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] HotWinterTavernText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (DaggerfallUnity.Instance.WorldTime.Now.IsNight)
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", out of the cold desert night, making your way closer to the fire...";
                else if (variant == 1)
                    raw = BuildName() + " seems to be the last bastion of warmth and light against the desertic darkness outside...";
                else
                    raw = "Rubbing your hands briskly together, you enter " + BuildName() + ". The fireplace is so grand in the room, in short time you feel almost too warm...";
            }

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "The bright colors of the interior of " + BuildName() + " add life to the otherwise gray day...";
                else if (variant == 1)
                    raw = "You enter " + BuildName() + " to the smell of fresh baked bread and the sound of the patrons laughing with the serving wench...";
                else
                    raw = "The serving wench is passing other patrons glasses of cold " + RandomAlcohol() + " as you enter " + BuildName() + " from the cloudy day...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " and shake the rain from your shoulders, hoping for a fire of some sort to dry yourself.";
                else if (variant == 1)
                    raw = "Winds and rain follow you into " + BuildName() + " like a beggar. You wipe your wet shoulders and head as you head for the fireplace.";
                else
                    raw = "Slightly shivering from the tempest, you escape into " + BuildName() + ". Other miserably wet people have gathered in dripping groups.";
            }
            else if (weatherID == 3) // Snowy
            {
                if (variant == 0)
                    raw = "You close the door on the snow and rub your hands together, warming them. Ahead of you the innkeeper of " + BuildName() + " hails you welcome, holding a glass of " + RandomAlcohol() + "...";
                else if (variant == 1)
                    raw = "Great billows of wind and snow follow you into " + BuildName() + " like a white shadow. The floor is wet with the footprints of the patrons...";
                else
                    raw = RandomAlcohol() + " is the featured drink today in " + BuildName() + ". You shake the snow from your shoulders and head, and think that nothing could sound better...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", feeling rather comfortable on this cool day...";
                else if (variant == 1)
                    raw = "It is a comfortably cool day, and the townspeople in " + BuildName() + " drink their wine to a toast for such a pleasant day...";
                else // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "The harsh sunlight shining through " + BuildName() + "'s windows makes you question if it was even worth coming here in the first place...";
                    else
                        raw = "The sunlight shining through " + BuildName() + "'s windows does as much as the roaring fireplace to warm your slightly chilled body...";
                }
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainFallTavernText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (DaggerfallUnity.Instance.WorldTime.Now.IsNight)
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", rubbing the cold night air from your bones. The tavern is full of people bustling here and there...";
                else if (variant == 1)
                    raw = "On this cold autumn night, " + BuildName() + " seems especially inviting. You smell apples baking in the kitchen and look forward to your first serving of " + RandomAlcohol() + "...";
                else
                    raw = "The warm smells of tobacco smoke and simple foods draw you into " + BuildName() + " from the cold autumn night...";
            }

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "The tavern you enter is brightly lit and colorful, compared to the cold, dark clouds that hang in the sky...";
                else if (variant == 1)
                    raw = "You walk out of the cloudy autumn day into " + BuildName() + ", the smell of baking bread pulling on you like a lure...";
                else
                    raw = "On an overcast autumn day like today, " + BuildName() + " is very popular and you can see why. It certainly lifts the spirits...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " and shake the cold rain from your shoulders, hoping for a fire of some sort to dry yourself...";
                else if (variant == 1)
                    raw = "Cold winds and rain follow you into " + BuildName() + " like a beggar. You wipe your wet shoulders and head as you head for the fireplace...";
                else
                    raw = "Shivering from the freezing autumn tempest, you escape into " + BuildName() + ". Other miserably wet people have gathered in dripping groups...";
            }
            else // Sunny or anything else
            {
                if (variant == 0) // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You enter " + BuildName() + ", heartened by the smells of food and spices, and prepare to relax in a nice dark corner away from the cruel sun...";
                    else
                        raw = "You enter " + BuildName() + ", heartened by the smells of food and spices, and prepare to relax in the warm sunlight...";
                }
                else if (variant == 1)
                    raw = "The fiery autumn sun disappears as you enter " + BuildName() + ". The sounds of laughter, clinking glasses, and banging cooking pans meet your ears...";
                else
                    raw = "The townsmen have met in " + BuildName() + " to celebrate a good fall harvest. Their exuberance is infectious. Oh great, you say to yourself, I'll probably have this stupid drinking song in my head all day...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainSpringTavernText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (DaggerfallUnity.Instance.WorldTime.Now.IsNight)
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", searching for a clear table at which you can relax on this cool spring night...";
                else if (variant == 1)
                    raw = BuildName() + " is like a beacon in the cool spring night, full of torch light, clinking glasses, and the aromas of the kitchen...";
                else
                    raw = "It is a cool spring night outside, but you have no need to seek out the fireplace on entering " + BuildName() + ". The tobacco smoke and smell of new bread trickles out the open windows and into the night...";
            }

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = BuildName() + "'s colorful interior adds life to the otherwise gray spring day...";
                else if (variant == 1)
                    raw = "Despite the rather cloudy conditions outside, the atmosphere inside " + BuildName() + " is extremely lively. Someone could even get hurt...";
                else
                    raw = "You leave the overcast spring day and enter " + BuildName() + ". The smell of simple baked breads coming from the kitchen seems like an invitation to stay.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " and wipe your rain soaked head on a small towel offered by the serving wench. Outside the rare spring shower continues...";
                else if (variant == 1)
                    raw = "You are dripping wet as you enter " + BuildName() + ", but then again, so are most of the rest of the patrons. The barwench is busily passing out towels as well as drinks...";
                else
                    raw = "You can still hear the cold spring tempest outside, beating against the walls of " + BuildName() + ". One of the maids stands by the door, attempting to mop up a persistent leak...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "The chill breath of spring follows you into " + BuildName() + ", bringing with it the smell of bloomed flowers and ice fresh air...";
                else if (variant == 1)
                    raw = "Through the open door and windows, all the smells of spring come into " + BuildName() + " and the freeze of the winter months fly out...";
                else // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You walk into " + BuildName() + ", thankful to have at least a moment of reprieve from the horribly sunny spring day...";
                    else
                        raw = "You walk into " + BuildName() + ", immediately wishing you were again out in the cool spring day...";
                }
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainSummerTavernText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (DaggerfallUnity.Instance.WorldTime.Now.IsNight)
            {
                if (variant == 0)
                    raw = "You are greeted by warm laughter and merry talk as you enter " + BuildName() + ". Torches around the tavern shed light on this cold summer night...";
                else if (variant == 1)
                    raw = "The wind and chill of the day have disappeared, and it has turned out to be a pleasantly cool night. The torches on the walls of " + BuildName() + " light up many a face...";
                else
                    raw = "The summer night is colder than the day had been, and you enter " + BuildName() + " craving something warm and dry...";
            }

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0) // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism())
                        raw = "While you are relieved the clouds shield you from the sun today, the air devoid of any warmth on this cold day has allowed your clothes to freeze to your clammy, unliving skin. As you close the door to " + BuildName() + " your eyes search for a warm dark corner in which to thaw out...";
                    else
                        raw = "Although the sun isn't shining, the chill has frozen your clothes to your back like a second skin. As you close the door to " + BuildName() + " your eyes search for a warm dark corner in which to rest...";
                }
                else if (variant == 1)
                    raw = "It is uncomfortably cold and dry in " + BuildName() + ", almost as bad as the chilly summer day outside...";
                else
                    raw = "You walk into " + BuildName() + " from the cold, cloudy day, welcomed by the other patrons sequestered away from the elements...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You shake the rain from your shoulders and enter " + BuildName() + ", hoping for a place where you can at last rest and dry out...";
                else if (variant == 1)
                    raw = "A burst of the cold summer rain follows you into " + BuildName() + ", and you follow the many paths of muddy footprints to the bar...";
                else
                    raw = "Dripping from the cold summer tempest outside, you enter " + BuildName() + ", relieved to have found shelter...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "Warm shade and warmer drinks greet you as you make your way out of the chill and into " + BuildName() + ", smiling at the serving wench who holds a glass of " + RandomAlcohol() + " ready for the first thirsty patron...";
                else if (variant == 1)
                    raw = "It is a relief to move inside " + BuildName() + ", away from the chilling summer air, and you feel that a cold mug of grog might be in order...";
                else
                    raw = "You enter " + BuildName() + ", feeling the cool shadows fall over your face like a blanket...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainWinterTavernText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (DaggerfallUnity.Instance.WorldTime.Now.IsNight)
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", out of the frozen winter night, making your way closer to the fire...";
                else if (variant == 1)
                    raw = BuildName() + " seems to be the last bastion of warmth and light against the arctic darkness outside...";
                else
                    raw = "Rubbing your hands briskly together, you enter " + BuildName() + ". The fireplace is so grand in the room, in short time you feel almost too warm...";
            }

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "The bright colors of the interior of " + BuildName() + " add life to the otherwise gray day...";
                else if (variant == 1)
                    raw = "You enter " + BuildName() + " and your frozen senses revive to the smell of fresh baked bread and the sound of the patrons laughing with the serving wench...";
                else
                    raw = "The serving wench is passing other patrons glasses of " + RandomAlcohol() + " as you enter " + BuildName() + " from the cold, cloudy day...";
            }
            else if (weatherID == 3) // Snowy
            {
                if (variant == 0)
                    raw = "You close the door on the snow and rub your hands together, warming them. Ahead of you the innkeeper of " + BuildName() + " hails you welcome, holding a glass of " + RandomAlcohol() + "...";
                else if (variant == 1)
                    raw = "Great billows of wind and snow follow you into " + BuildName() + " like a white shadow. The floor is wet with the footprints of the patrons...";
                else
                    raw = RandomAlcohol() + " is the featured drink today in " + BuildName() + ". You shake the snow from your shoulders and head, and think that nothing could sound better...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", feeling the hearth fire slowly chasing away the chill that has crept into your bones from this winter's day...";
                else if (variant == 1)
                    raw = "It is a freezing day for winter, and the red-cheeked townspeople in " + BuildName() + " drink their " + RandomAlcohol() + " to a toast for a long hibernation...";
                else // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "The harsh sunlight shining through " + BuildName() + "'s windows makes you question if it was even worth coming here in the first place...";
                    else
                        raw = "The winter sunlight shining through " + BuildName() + "'s windows helps little compared to the roaring fireplace to warm your chilled body...";
                }
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateFallTavernText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (DaggerfallUnity.Instance.WorldTime.Now.IsNight)
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", rubbing the chill night air from your bones. The tavern is full of people bustling here and there...";
                else if (variant == 1)
                    raw = "On this cool autumn night, " + BuildName() + " seems especially inviting. You smell apples baking in the kitchen and look forward to your first serving of " + RandomAlcohol() + "...";
                else
                    raw = "The warm smells of tobacco smoke and simple foods draw you into " + BuildName() + " from the cold autumn night...";
            }

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "The tavern you enter is brightly lit and colorful, compared to the cold, dark clouds that hang in the sky...";
                else if (variant == 1)
                    raw = "You walk out of the cloudy autumn day into " + BuildName() + ", the smell of baking bread pulling on you like a lure...";
                else
                    raw = "On an overcast autumn day like today, " + BuildName() + " is very popular and you can see why. It certainly lifts the spirits...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " and shake the chill rain from your shoulders, hoping for a fire of some sort to dry yourself...";
                else if (variant == 1)
                    raw = "Cold wind and rain follow you into " + BuildName() + " like a beggar. You wipe your wet shoulders and head as you head for the fireplace...";
                else
                    raw = "Shivering from the cold autumn tempest, you escape into " + BuildName() + ". Other miserably wet people have gathered in dripping groups...";
            }
            else // Sunny or anything else
            {
                if (variant == 0) // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You enter " + BuildName() + ", heartened by the smells of food and spices, and prepare to relax in a nice dark corner away from the cruel sun...";
                    else
                        raw = "You enter " + BuildName() + ", heartened by the smells of food and spices, and prepare to relax in the warm sunlight...";
                }
                else if (variant == 1)
                    raw = "The fiery autumn sun disappears as you enter " + BuildName() + ". The sounds of laughter, clinking glasses, and banging cooking pans meet your ears...";
                else
                    raw = "The townsmen have met in " + BuildName() + " to celebrate a good fall harvest. Their exuberance is infectious. Oh great, you say to yourself, I'll probably have this stupid drinking song in my head all day...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateSpringTavernText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (DaggerfallUnity.Instance.WorldTime.Now.IsNight)
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", searching for a clear table at which you can relax on this warm spring night...";
                else if (variant == 1)
                    raw = BuildName() + " is like a beacon in the cool spring night, full of torch light, clinking glasses, and the aromas of the kitchen...";
                else
                    raw = "It is a cool spring night outside, but you have no need to seek out the fireplace on entering " + BuildName() + ". The tobacco smoke and smell of new bread trickles out the open windows and into the night...";
            }

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = BuildName() + "'s colorful interior adds life to the otherwise gray spring day...";
                else if (variant == 1)
                    raw = "Despite the rather cloudy conditions outside, the atmosphere inside " + BuildName() + " is extremely lively. Someone could even get hurt...";
                else
                    raw = "You leave the overcast spring day and enter " + BuildName() + ". The smell of simple baked breads coming from the kitchen seems like an invitation to stay.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " and wipe your rain soaked head on a small towel offered by the serving wench. Outside the spring shower continues...";
                else if (variant == 1)
                    raw = "You are dripping wet as you enter " + BuildName() + ", but then again, so are most of the rest of the patrons. The barwench is busily passing out towels as well as drinks...";
                else
                    raw = "You can still hear the spring tempest outside, beating against the walls of " + BuildName() + ". One of the maids stands by the door, attempting to mop up a persistent leak...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "A cool breeze follows you into " + BuildName() + ", bringing with it the smell of newly bloomed flowers and fresh cut grass...";
                else if (variant == 1)
                    raw = "Through the open door and windows, all the smells of spring come into " + BuildName() + " and the stuffiness of the winter months fly out...";
                else // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You walk into " + BuildName() + ", thankful to have at least a moment of reprieve from the horribly sunny spring day...";
                    else
                        raw = "You walk into " + BuildName() + ", immediately wishing you were again out in the sunny spring day...";
                }
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateSummerTavernText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (DaggerfallUnity.Instance.WorldTime.Now.IsNight)
            {
                if (variant == 0)
                    raw = "You are greeted by warm laughter and merry talk as you enter " + BuildName() + ". Torches around the tavern shed light on this warm summer night...";
                else if (variant == 1)
                    raw = "The heat and humidity of the day have disappeared, and it has turned out to be a pleasantly cool night. The torches on the walls of " + BuildName() + " light up many a sunburned face...";
                else
                    raw = "The summer night is almost as warm as the day had been, and you enter " + BuildName() + " craving something cold and wet...";
            }

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "Although the sun isn't shining, the humidity has plastered your clothes to your back like a second skin. As you close the door to " + BuildName() + " your eyessearch for a cool dark corner in which to rest...";
                else if (variant == 1)
                    raw = "It is uncomfortably hot and sticky in " + BuildName() + ", almost as bad as the cloudy summer day outside...";
                else
                    raw = "You walk into " + BuildName() + " from the hot, cloudy day, welcomed by the other patrons sequestered away from the elements...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You shake the rain from your shoulders and enter " + BuildName() + ", hoping for a place where you can at last rest and dry out...";
                else if (variant == 1)
                    raw = "A burst of the warm summer rain follows you into " + BuildName() + ", and you follow the many paths of muddy footprints to the bar...";
                else
                    raw = "Dripping from the summer tempest outside, you enter " + BuildName() + ", relieved to have found shelter...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "Cool shade and cooler drinks greet you as you make your way out of the heat and into " + BuildName() + ", smiling at the serving wench who holds a glass of " + RandomAlcohol() + " ready for the first thirsty patron...";
                else if (variant == 1)
                    raw = "It is a relief to move inside " + BuildName() + ", away from the burning summer heat, and you feel that a cold mug of grog might be in order...";
                else
                    raw = "You enter " + BuildName() + ", feeling the cool shadows fall over your face like a blanket...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateWinterTavernText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (DaggerfallUnity.Instance.WorldTime.Now.IsNight)
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", out of the cold winter night, making your way closer to the fire...";
                else if (variant == 1)
                    raw = BuildName() + " seems to be the last bastion of warmth and light against the arctic darkness outside...";
                else
                    raw = "Rubbing your hands briskly together, you enter " + BuildName() + ". The fireplace is so grand in the room, in short time you feel almost too warm...";
            }

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "The bright colors of the interior of " + BuildName() + " add life to the otherwise gray day...";
                else if (variant == 1)
                    raw = "You enter " + BuildName() + " and your frozen senses revive to the smell of fresh baked bread and the sound of the patrons laughing with the serving wench...";
                else
                    raw = "The serving wench is passing other patrons glasses of " + RandomAlcohol() + " as you enter " + BuildName() + " from the cold, cloudy day...";
            }
            else if (weatherID == 3) // Snowy
            {
                if (variant == 0)
                    raw = "You close the door on the snow and rub your hands together, warming them. Ahead of you the innkeeper of " + BuildName() + " hails you welcome, holding a glass of " + RandomAlcohol() + "...";
                else if (variant == 1)
                    raw = "Great billows of wind and snow follow you into " + BuildName() + " like a white shadow. The floor is wet with the footprints of the patrons...";
                else
                    raw = RandomAlcohol() + " is the featured drink today in " + BuildName() + ". You shake the snow from your shoulders and head, and think that nothing could sound better...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", feeling the hearth fire slowly chasing away the chill that has crept into your bones from this winter's day...";
                else if (variant == 1)
                    raw = "It is a pleasant day for winter, and the red-cheeked townspeople in " + BuildName() + " drink their " + RandomAlcohol() + " to a toast for a long hibernation...";
                else // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "The harsh sunlight shining through " + BuildName() + "'s windows makes you question if it was even worth coming here in the first place...";
                    else
                        raw = "The winter sunlight shining through " + BuildName() + "'s windows does as much as the roaring fireplace to warm your chilled body...";
                }
            }
            return TextTokenFromRawString(raw);
        }

        #endregion

        #region Temples Text

        public static TextFile.Token[] HotFallTempleText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", hoping that your spirits can be lifted on this comfortable, overcast day. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "It is a miserable autumn day, but within " + BuildName() + ", there is faith yet that the winter may not be so deadly...";
                else
                    raw = "Warm, dark clouds hang over " + BuildName() + ", but within you find all is bright and warmer. Billowing clouds of perfumed incense greet your breath.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter the warm " + BuildName() + ", shaking the cool rain off of yourself. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "Wet from the autumn thunderstorm, you enter " + BuildName() + ", dripping. The warm smell of incense begins to invigorate you, and in the distance, you can hear " + BuildName() + " clergy at prayer.";
                else
                    raw = BuildName() + " sanctuary is a place of peace. Only by concentrating on the world outside yourself can you hear the vague drumbeat of the autumn thunderstorm outside.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " and a comfortable fall breeze follows you into the hallowed halls. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "The musty smell of autumn in the outside air becomes mixed with the spicy odor of incense as you enter " + BuildName() + " sanctuary...";
                else
                    raw = "The stained glass windows in " + BuildName() + " sanctuary catch the rays from the bright autumn sun and transform them into crystalline fire.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] HotSpringTempleText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0) // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You enter " + BuildName() + " thankful that today you are given mercy from the sun by the thick clouds covering the sky. You can hear chanting in the distance...";
                    else
                        raw = "You enter " + BuildName() + " wishing that the sun would return and make the day a truly pleasant one. You can hear chanting in the distance...";
                }
                else if (variant == 1) // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You walk into " + BuildName() + ", smelling the sweet incense that rises like a cloud from the sanctuary floor. In the distance, you hear the clergy praying for a return of the sun to this dreary spring day. You silenty curse the priests and their wishes for that dreaded tormentor to return...";
                    else
                        raw = "You walk into " + BuildName() + ", smelling the sweet incense that rises like a cloud from the sanctuary floor. In the distance, you hear the clergy praying for a return of the sun to this dreary spring day...";
                }
                else
                    raw = "You enter " + BuildName() + ", gray as this spring day sky. Almost immediately the bittersweet smell of the incense strengthens your spirit. The clergy drone their prayers far off in the distance.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", shaking the warm rain off of yourself. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "Trailing puddles of warm spring rain behind you, you enter " + BuildName() + " sanctuary. An open window lets the smell of the storm in, overpowering the sweet incense of " + BuildName() + " itself.";
                else
                    raw = "Wiping the warm rain from your shoulders and head, you walk into " + BuildName() + " sanctuary. Off to the distance, you can hear the clergy, prayer in a sing-song for a good planting season for the farmers...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " and a cool spring breeze follows you, bringing with it the smell of fresh blooms. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "It is bright and fresh in " + BuildName() + " sanctuary as the spring day outside. In the distance, you can hear the chanting of the clergy.";
                else
                    raw = "The smell of the new blossoms of spring that came into " + BuildName() + " sanctuary with you mixes with the slightly spicy smell of incense. You can hear the clergy far away, giving a pray of thanksgiving for the beauty of the new year.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] HotSummerTempleText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", hoping that your spirits can be lifted on this gray, overcast day. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "As miserable as the sweltering summer day is, it is at least less eerie than the sterile " + BuildName() + " sanctuary. No birds or insects can be heard in here, and the air is empty of both smell and temperature. It may be very holy indeed, but you feel like you have stepped into a void.";
                else
                    raw = BuildName() + " sanctuary is a haven for all, a place of self reflection, solitude, and prayer. It is always perfectly mild in here, as hot and cloudy as the day outside may be...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", shaking the rain off of yourself. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "You enter the hallowed ground of " + BuildName() + " sanctuary, the sounds of the summer rain shower mixing with that of the chanting clergy. All your tension, like the rare water, flows away.";
                else // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "A fine mist, escaped from the tempest outside, has flooded " + BuildName() + " sanctuary bringing all the smells of summer with it. You recognize the sing-song prayer of the clergy as a plea for a benevolent summer sun. Even though you prefer not being soaked by rain, you rather that than being cooked by that menace in the sky...";
                    else
                        raw = "A fine mist, escaped from the tempest outside, has flooded " + BuildName() + " sanctuary bringing all the smells of summer with it. You recognize the sing-song prayer of the clergy as a plea for a benevolent summer sun.";
                }
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", thankful for the respite from the harsh summer sun. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "The stinging of prickly heat is instantly soothed the moment you enter " + BuildName() + " sanctuary. In the distance, you can hear the low murmur of chanting. This is a place of great tranquillity.";
                else
                    raw = "The god who protects " + BuildName() + " must be great indeed, for it is as bright as the summer day yet comfortable as a shadowy place within. A pleasant floral odor of incense fills the air.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] HotWinterTempleText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", hoping that your spirits can be lifted on this cool, overcast day. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "Already you can feel the power of " + BuildName() + " sanctuary, warming your cold body, brightening your spirits which have grown dark as the cloud filled sky...";
                else
                    raw = "Strange sounds and smells meet you as you leave the cool, cloudy day the incense's bittersweet aroma is new to your senses and the clergy of " + BuildName() + " sing solemnly in a language dead for many eons...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", shaking the cold rain off of yourself. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "Trailing puddles of cold rain behind behind you, you enter " + BuildName() + " sanctuary.An open window lets the smell of the storm in, overpowering the sweet incense of " + BuildName() + " itself.";
                else
                    raw = "Wiping the cold rain from your shoulders and head, you walk into " + BuildName() + " sanctuary. Off to the distance, you can hear the clergy, prayer in a sing-song for a good planting in spring for the farmers...";
            }
            else if (weatherID == 3) // Snowy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", shaking the snow off of yourself and stamping your feet to warm them. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "Brushing the cold off you walk into " + BuildName() + " sanctuary. The smell of incense strikes you immediately, spicy and somewhat sweet...";
                else
                    raw = "You trail some cold into " + BuildName() + " sanctuary as you enter. The air is as cold as the outdoors, yet filled with the smell of incense and the sound of the clergy praying...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " and a cool breeze follows you into the hallowed halls. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = BuildName() + " sanctuary is as austerely beautiful as the bright day outside. Tendrils of sweet incense rise from the floor as if in greeting...";
                else
                    raw = "On a day as cool as it is blindingly bright, it is easy to see why this part of " + BuildName() + " is called the sanctuary. Far off to the distance, you can hear the clergy at prayer.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainFallTempleText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", hoping that your spirits can be lifted on this cold, overcast day. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "It is a miserable autumn day, but within " + BuildName() + ", there is faith yet that the winter may not be so deadly...";
                else
                    raw = "Cold, dark clouds hang over " + BuildName() + ", but within you find all is bright and warm. Billowing clouds of perfumed incense greet the fog of your breath.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", shaking the cold rain off of yourself. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "Wet from the autumn thunderstorm, you enter " + BuildName() + ", dripping. The warm smell of incense begins to invigorate you, and in the distance, you can hear " + BuildName() + " clergy at prayer.";
                else
                    raw = BuildName() + " sanctuary is a place of peace. Only by concentrating on the world outside yourself can you hear the vague drumbeat of the autumn thunderstorm outside.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " and a chill fall breeze follows you into the hallowed halls. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "The musty smell of autumn in the outside air becomes mixed with the spicy odor of incense as you enter " + BuildName() + " sanctuary...";
                else
                    raw = "The stained glass windows in " + BuildName() + " sanctuary catch the rays from the bright autumn sun and transform them into crystalline fire.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainSpringTempleText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0) // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You enter " + BuildName() + " thankful that today you are given mercy from the sun by the thick clouds covering the sky. You can hear chanting in the distance...";
                    else
                        raw = "You enter " + BuildName() + " wishing that the sun would return and make the day a truly pleasant one. You can hear chanting in the distance...";
                }
                else if (variant == 1) // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You walk into " + BuildName() + ", smelling the sweet incense that rises like a cloud from the sanctuary floor. In the distance, you hear the clergy praying for a return of the sun to this dreary and cold spring day. You silenty curse the priests and their wishes for that dreaded tormentor to return...";
                    else
                        raw = "You walk into " + BuildName() + ", smelling the sweet incense that rises like a cloud from the sanctuary floor. In the distance, you hear the clergy praying for a return of the sun to this dreary and cold spring day...";
                }
                else
                    raw = "You enter " + BuildName() + ", gray as this spring day sky. Almost immediately the bittersweet smell of the incense strengthens your spirit. The clergy drone their prayers far off in the distance.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", shaking the cool rain off of yourself. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "Trailing puddles of cool spring rain behind you, you enter " + BuildName() + " sanctuary.An open window lets the smell of the storm in, overpowering the sweet incense of " + BuildName() + " itself.";
                else
                    raw = "Wiping the cool rain from your shoulders and head, you walk into " + BuildName() + " sanctuary. Off to the distance, you can hear the clergy, prayer in a sing-song for a good planting season for the farmers...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " and a chill spring breeze follows you, bringing with it the smell of fresh air. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "It is bright and fresh in " + BuildName() + " sanctuary as the spring day outside. In the distance, you can hear the chanting of the clergy.";
                else
                    raw = "The smell of the fresh mountain air of spring that came into " + BuildName() + " sanctuary with you mixes with the slightly spicy smell of incense. You can hear the clergy far away, giving a pray of thanksgiving for the beauty of the new year.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainSummerTempleText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", hoping that your spirits can be lifted on this gray, overcast day. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "As miserable as the sweltering summer day is, it is less eerie than the sterile " + BuildName() + " sanctuary. No birds or insects can be heard in here, and the air is empty of both smell and temperature. It may be very holy indeed, but you feel like you have stepped into a void.";
                else
                    raw = BuildName() + " sanctuary is a haven for all, a place of self reflection, solitude, and prayer. It is always perfectly mild in here, as cold and cloudy as the day outside may be...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", shaking the rain off of yourself. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "You enter the hallowed ground of " + BuildName() + " sanctuary, the sounds of the summer rain shower mixing with that of the chanting clergy. All your tension like water flows away.";
                else
                    raw = "A fine mist, escaped from the tempest outside, has flooded " + BuildName() + " sanctuary bringing all the smells of summer with it. You recognize the sing-song prayer of the clergy as a plea for a benevolent winter sun.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", thankful for the respite from the harsh summer sun and cold winds. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "The stinging of prickly heat is instantly soothed the moment you enter " + BuildName() + " sanctuary. In the distance, you can hear the low murmur of chanting. This is a place of great tranquillity.";
                else
                    raw = "The god who protects " + BuildName() + " must be great indeed, for it is as bright as the summer day yet comfortable as a shadowy place within. A pleasant floral odor of incense fills the air.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainWinterTempleText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", rubbing your hands to warm them and hoping that your spirits can be lifted on this cold, overcast day. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "Already you can feel the power of " + BuildName() + " sanctuary, thawing your frigid body, brightening your spirits which have grown dark as the cloud filled sky...";
                else
                    raw = "Strange sounds and smells meet you as you leave the cold, cloudy winter's day: the incense's bittersweet aroma is new to your senses and the clergy of " + BuildName() + " sing solemnly in a language dead for many eons...";
            }
            else if (weatherID == 3) // Snowy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", shaking the cold off of yourself and stamping your feet to warm them. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "Brushing the snow off your shoulders and head, you walk into " + BuildName() + " sanctuary. The smell of incense strikes you immediately, spicy and somewhat sweet...";
                else
                    raw = "You trail some snow into " + BuildName() + " sanctuary as you enter. The air is as cold as outside here, yet filled with the smell of incense and the sound of the clergy praying...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", rubbing your hands together to warm them on this frozen winter's day. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = BuildName() + " sanctuary is as austerely beautiful as the bright winter's day outside. Tendrils of sweet incense rise from the floor as if in greeting...";
                else
                    raw = "On a day as cold as it is blindingly bright, it is easy to see why this part of " + BuildName() + " is called the sanctuary. Far off to the distance, you can hear the clergy at prayer.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateFallTempleText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", hoping that your spirits can be lifted on this cold, overcast day. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "It is a miserable autumn day, but within " + BuildName() + ", there is faith yet that the winter may not be so deadly...";
                else
                    raw = "Cold, dark clouds hang over " + BuildName() + ", but within you find all is bright and warm. Billowing clouds of perfumed incense greet the fog of your breath.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", shaking the cold rain off of yourself. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "Wet from the autumn thunderstorm, you enter " + BuildName() + ", dripping. The warm smell of incense begins to invigorate you, and in the distance, you can hear " + BuildName() + " clergy at prayer.";
                else
                    raw = BuildName() + " sanctuary is a place of peace. Only by concentrating on the world outside yourself can you hear the vague drumbeat of the autumn thunderstorm outside.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " and a cool fall breeze follows you into the hallowed halls. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "The musty smell of autumn in the outside air becomes mixed with the spicy odor of incense as you enter " + BuildName() + " sanctuary...";
                else
                    raw = "The stained glass windows in " + BuildName() + " sanctuary catch the rays from the bright autumn sun and transform them into crystalline fire.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateSpringTempleText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0) // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You enter " + BuildName() + " thankful that today you are given mercy from the sun by the thick clouds covering the sky. You can hear chanting in the distance...";
                    else
                        raw = "You enter " + BuildName() + " wishing that the sun would return and make the day a truly pleasant one. You can hear chanting in the distance...";
                }
                else if (variant == 1) // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You walk into " + BuildName() + ", smelling the sweet incense that rises like a cloud from the sanctuary floor. In the distance, you hear the clergy praying for a return of the sun to this dreary spring day. You silenty curse the priests and their wishes for that dreaded tormentor to return...";
                    else
                        raw = "You walk into " + BuildName() + ", smelling the sweet incense that rises like a cloud from the sanctuary floor. In the distance, you hear the clergy praying for a return of the sun to this dreary spring day...";
                }
                else
                    raw = "You enter " + BuildName() + ", gray as this spring day sky. Almost immediately the bittersweet smell of the incense strengthens your spirit. The clergy drone their prayers far off in the distance.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", shaking the warm rain off of yourself. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "Trailing puddles of warm spring rain behind you, you enter " + BuildName() + " sanctuary. An open window lets the smell of the storm in, overpowering the sweet incense of " + BuildName() + " itself.";
                else
                    raw = "Wiping the warm rain from your shoulders and head, you walk into " + BuildName() + " sanctuary. Off to the distance, you can hear the clergy, prayer in a sing-song for a good planting season for the farmers...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + " and a cool spring breeze follows you, bringing with it the smell of fresh blooms. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "It is bright and fresh in " + BuildName() + " sanctuary as the spring day outside. In the distance, you can hear the chanting of the clergy.";
                else
                    raw = "The smell of the new blossoms of spring that came into " + BuildName() + " sanctuary with you mixes with the slightly spicy smell of incense. You can hear the clergy far away, giving a pray of thanksgiving for the beauty of the new year.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateSummerTempleText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", hoping that your spirits can be lifted on this gray, overcast day. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "As miserable as the sweltering summer day is, it is at least less eerie than the sterile " + BuildName() + " sanctuary. No birds or insects can be heard in here, and the air is empty of both smell and temperature. It may be very holy indeed, but you feel like you have stepped into a void.";
                else
                    raw = BuildName() + " sanctuary is a haven for all, a place of self reflection, solitude, and prayer. It is always perfectly mild in here, as hot and cloudy as the day outside may be...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", shaking the rain off of yourself. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "You enter the hallowed ground of " + BuildName() + " sanctuary, the sounds of the summer rain shower mixing with that of the chanting clergy. All your tension like water flows away.";
                else
                    raw = "A fine mist, escaped from the tempest outside, has flooded " + BuildName() + " sanctuary bringing all the smells of summer with it. You recognize the sing-song prayer of the clergy as a plea for a bountiful fall harvest.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", thankful for the respite from the harsh summer sun. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "The stinging of prickly heat is instantly soothed the moment you enter " + BuildName() + " sanctuary. In the distance, you can hear the low murmur of chanting. This is a place of great tranquillity.";
                else
                    raw = "The god who protects " + BuildName() + " must be great indeed, for it is as bright as the summer day yet comfortable as a shadowy place within. A pleasant floral odor of incense fills the air.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateWinterTempleText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", rubbing your hands to warm them and hoping that your spirits can be lifted on this cold, overcast day. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "Already you can feel the power of " + BuildName() + " sanctuary, thawing your frigid body, brightening your spirits which have grown dark as the cloud filled sky...";
                else
                    raw = "Strange sounds and smells meet you as you leave the cold, cloudy winter's day: the incense's bittersweet aroma is new to your senses and the clergy of " + BuildName() + " sing solemnly in a language dead for many eons...";
            }
            else if (weatherID == 3) // Snowy
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", shaking the snow off of yourself and stamping your feet to warm them. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = "Brushing the snow off your shoulders and head, you walk into " + BuildName() + " sanctuary. The smell of incense strikes you immediately, spicy and somewhat sweet...";
                else
                    raw = "You trail some snow into " + BuildName() + " sanctuary as you enter. The air is as cold as outside here, yet filled with the smell of incense and the sound of the clergy praying...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter " + BuildName() + ", rubbing your hands together to warm them on this bright winter's day. You can hear chanting in the distance...";
                else if (variant == 1)
                    raw = BuildName() + " sanctuary is as austerely beautiful as the bright winter's day outside. Tendrils of sweet incense rise from the floor as if in greeting...";
                else
                    raw = "On a day as cold as it is blindingly bright, it is easy to see why this part of " + BuildName() + " is called the sanctuary. Far off to the distance, you can hear the clergy at prayer.";
            }
            return TextTokenFromRawString(raw);
        }

        #endregion

        #region Mages Guild Text

        public static TextFile.Token[] DesertFallMagesText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter the Mage's Guild, its gloomy interior not much different from the chilly, overcast sky outside. At least, you silently thank, the interior is warm. You rub your hands together to bring back some feeling. Around you are arcane implements and mystical apparati. You feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "On a dark cool day like today, those who devote their lives to the mystical sciences are at their busiest. Outside, people look into the sky and view the coming winter with dread. All in the Mages Guild are as active with mystic energy as they might be on any day, including the books and relics that line the shelves.";
                else
                    raw = "You enter the Mages Guild, going from a cool gloomy outside to a cool gloomy inside. Because of the shadows, you cannot see all that the chamber contains. You suspect that even by the light of a bright day, the room has its secrets. Potions, relics, and books line the bookshelves shrouded by a fine dust.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You shake the hot rain from your shoulders as you enter the Mages Guild, stamping your feet to bring back some warmth. The dry interior is a welcomed sight after standing in the uncommon downpour outside. Around you are arcane implements and mystical apparati. You feel a slight tingling on your skin.";
                else if (variant == 1)
                    raw = "The Mages Guild is dry, but no more welcome than the hot autumn tempest outside. Strange and exotic potions, ancient scrolls, and arcane relics look down at you from the shelves as if they are predatory birds, watching for you to make a mistake. The dark energy in the room goes through you like a charge.";
                else
                    raw = "All along the walls of the Mages Guild, blackened with soot and neglect, are shelves of books and arcane apparati so cloaked in dust to be practically indistinguishable one from the other. You can hear the sound of the desert winds outside, uncomfortably loud in the normally still little room.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "Behind you the fall sun sheds heat upon the land, holding back the chill fingers of winter yet another day. Around you are arcane implements and mystical apparati. You feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "While the local farmers have been gathering their crops, it would appear the mages have been collecting a harvest of their own. The Mages Guild is crowded with scrolls, relics, charms, and artifacts. Clearly some extensive trading between one town's guild to another's has been going on this fall.";
                else
                    raw = "You enter the Mages Guild, a world where nature holds little power. Outside the sun beats the land with heat, searing it clean. In here however, strange flora lives year round, competing for space next to the scrolls, potions, and other mystic apparati that crowds the Guild's shelves.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] DesertSpringMagesText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 4);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "Although the sky is gray the weather itself is quite hot. You can smell dry heat in the air, but that quickly changes as the door shuts behind you. The Mages Guild smells like old spices and chemicals. You can make out dozens of arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "You enter the Mages Guild from the gloomy spring day and are struck by the clutter. You know that wizardry is a mental art, and it is hard to believe the sorcerers behind this jumble of items and apparati are not either madmen or fools. You can definitely hear small living things move about in the piles.";
                else
                    raw = "The Mages Guild is not a place where the normal gods of Nature hold reign, but on such a bleak spring day, it does not look like they have power anywhere. Somehow the chamber is simultaneously too hot and too cold, wet with humidity in one spot, drier than the Khajiit desert in another. Even the mystical items on the shelves are juxtaposed between extremes, ancient scrolls next to modern measuring devices, this one filthy with neglect, that polished with care.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You shake the hot rain from your shoulders, conscious of the pooling water at your feet. As your eyes slowly adjust to the dim interior, you can make out arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "As thick as the stone walls of the Mages guild are, you can hear virtually every splatter of the hot spring rain outside. You are almost afraid to talk, for every echo of each footstep causes great clouds of dust to billow. And there are books and apparati in this room, you suspect, that should never be disturbed.";
                else
                    raw = "An open window in the Mages Guild lets in the smell of the spring storm, which mingles with the chemical stench that bubbles from an open cauldron. The chamber is filled with arcane objects, jugs, cloaks, scrolls, amulets, tomes, brooches, potions, robes, figurines, talismans.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "A cool breeze follows you into the Mages Guild, drying the slight sheen of sweat from your brow. The interior smells of spices and chemicals. As your eyes adjust to the dim light you can make out arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "You enter the Mages Guild, the smell of sulfur and rotting parchment replacing that of new cacti blossoms. Scrolls and potions, relics and artifacts vie for space on the bookshelves. You can hear small living things scuttling behind the volumes of alien encyclopedia.";
                else if (variant == 2)
                    raw = "As you enter the Mage's Guild the nearest enchanter yells at you, urging you to shake off the sand from the outside that got stuck on your clothes and boots - 'Can't have you ruining all the fragile texts we guard here, can we?'";
                else
                    raw = "The mystical energy in the Mages Guild runs through you like a charge of lightning. Outside nature is in her full vernal glory on this sunny day, but she has little power in here. From the stone walls to the scrolls and potions, the most mundane parts of this chamber seem like phantasmagoria.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] DesertSummerMagesText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter the Mages Guild, its gloomy interior not much different from the iron-gray, overcast sky outside. At least, you silently think, the sun isn't out. Around you are arcane implements and mystical apparati. You feel a slight tingling on your skin.";
                else if (variant == 1)
                    raw = "All the humidity and heat of the day are intensified in the Mages Guild. To add to the infernal atmosphere, tendrils of soot-filled smoke and the whimpers from some doomed experiment slip through the cracks of locked doors. Each book on the shelf looks like it is watching you.";
                else
                    raw = "You enter the Mages Guild, an amalgamation of primordial relics, moldering scrolls, dripping unguents, and smoking fluids. An occasional groan or minor explosion can be heard coming from the laboratories. At least it's not as hot and humid as outside.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You shake the hot rain from your shoulders as you enter the Mages Guild. The dry interior is a welcomed sight after standing in the downpour outside. Around you are arcane implements and mystical apparati. You feel a slight tingling on your skin.";
                else if (variant == 1)
                    raw = "The sound of the brief summer rain is muffled by the thick walls and thicker velvet curtains. This is a place where the outside world is a distraction to be avoided. The smells, sights, and sounds here are unique to the field of mystical experimentation.";
                else
                    raw = "You enter the Mages Guild, avoiding the streams of water that run unabsorbed by the parched desert sands. The room is damp, but the thick stone walls keep it cool even in the dog days of summer. Strange devices and venerable tomes crowd across the dust and soot streaked shelves.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You wipe the sweat off your brow as you enter the Mages Guild. The cool shade of the interior is a welcomed sight after standing under the harsh summer sun. Around you are arcane implements and mystical apparati. You feel a slight tingling on your skin.";
                else if (variant == 1)
                    raw = "For a moment after leaving the bright summer sunshine, your eyes register only darkness in the gloomy Mages Guild. When they adjust, you can understand very little of what you see. Tomes of antiquated wizardry and obscure objects crowd the dusty shelves.";
                else
                    raw = "The door to the Mages Guild opens with a puff of dust that sticks to the sweat on your sunburned face. The smells of sulfurous potions burning and ancient scrolls of moldering parchment sting your nose. At least it is cool in here, you say to yourself.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] DesertWinterMagesText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "The gray sky has done little to warm your bones, which by now are chill indeed. You enter the Mages Guild, stamping your feet to increase the circulation and crowd close to a brazier that glows hot with fiery coals. As the warmth slowly seeps in, you notice arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "It is a perfect day for those who practice the shadowy arts: the Mages Guild comes most alive on chill, dark days. Cauldrons bubble on the fireplace, filling the room with translucent smoke that smells of animal fat and sulfur. The shelves are cluttered with mystical books and artifacts...";
                else
                    raw = "You know that you can only see half of what is in the Mages Guild. The titles of the books on the shelves seem to change depending on the angle you look. Invisible fingers run through the dust and over your face. From the corner of your eye, you can see demons grinning at you, but they vanish when you turn to look at them. As dark a day as it is outside, you want to do your business here and leave...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You shake the cool rain from your shoulders, conscious of the pooling water at your feet. As your eyes slowly adjust to the dim interior, you can make out arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "As thick as the stone walls of the Mages guild are, you can hear virtually every splatter of the rain outside. You are almost afraid to talk, for every echo of each footstep causes great clouds of dust to billow. And there are books and apparati in this room, you suspect, that should never be disturbed.";
                else
                    raw = "An open window in the Mages Guild lets in the smell of the  storm, which mingles with the chemical stench that bubbles from an open cauldron. The chamber is filled with arcane objects, jugs, cloaks, scrolls, amulets, tomes, brooches, potions, robes, figurines, talismans.";
            }
            else if (weatherID == 3) // Snowy
            {
                if (variant == 0)
                    raw = "Chill winds swirl about you as you enter the Mages Guild and shut the door. Outside the winter's cold sets in without any signs of stopping. You stamp your feet and crowd close to a brazier that glows hot with fiery coals. As the warmth slowly seeps in, you notice arcane implements and mystical apparati. You think you feel a strange tingling over your skin.";
                else if (variant == 1)
                    raw = "The Mages Guild is so omnipresent that all the arcane relics within seem to be covered in a thin shroud. It is an almost soundless chamber, with only an occasional sigh from the pot of boiling liquid on the fire. Slowly warmth creeps back into your chilled body.";
                else
                    raw = "You enter the dim Mages Guild, warmed by a spell that brings life back into your chilled body almost immediately. Books, charms, scrolls, and other relics, older than human memory, are kept in pristine condition here by the same mystic energy.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "A cool breeze follows you into the Mages Guild, drying the slight sheen of sweat from your brow. The interior smells of spices and chemicals. As your eyes adjust to the dim light you can make out arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "As sunny as it was, you felt a cool chill outside and now, within the Mages Guild, you feel the cold more intensely. Dust lies on the shelves of books, potions, and relics like an early frost.";
                else
                    raw = "The melancholy Mages Guild is slightly warmer than the day outside but, without the sunlight, the effect is much the same. Many books and arcane instruments lie scattered about, but they fail to lend any vitality to the scene. It is a place of mystical study, not built for human comfort.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] SwampFallMagesText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter the Mage's Guild, its gloomy interior not much different from the chilly, overcast sky outside. At least, you silently thank, the interior is warm. You rub your hands together to bring back some feeling. Around you are arcane implements and mystical apparati. You feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "On a dark cool day like today, those who devote their lives to the mystical sciences are at their busiest. Outside, people look into the sky and view the coming winter with dread. All in the Mages Guild are as ctive with mystic energy as they might be on any day, including the books and relics that line the shelves.";
                else
                    raw = "You enter the Mages Guild, going from a cool gloomy outside to a cool gloomy inside. Because of the shadows, you cannot see all that the chamber contains. You suspect that even by the light of a bright day, the room has its secrets. Potions, relics, and books line the bookshelves shrouded by a fine dust.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You shake the hot rain from your shoulders as you enter the Mages Guild, stamping your feet to bring back some warmth. The dry interior is a welcomed sight after standing in the uncommon downpour outside. Around you are arcane implements and mystical apparati. You feel a slight tingling on your skin.";
                else if (variant == 1)
                    raw = "The Mages Guild is dry, but no more welcome that the hot autumn tempest outside. Strange and exotic potions, ancient scrolls, and arcane relics look down at you from the shelves as if they are predatory birds, watching for you to make a mistake. The dark energy in the room goes through you like a charge.";
                else
                    raw = "All along the walls of the Mages Guild, blackened with soot and neglect, are shelves of books and arcane apparati so cloaked in dust to be practically indistinguishable one from the other. You can hear the sound of the downpour outside, uncomfortably loud in the normally still little room.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "Behind you the fall sun sheds heat upon the land, holding back the chill fingers of winter yet another day. Around you are arcane implements and mystical apparati. You feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "While the local farmers have been gathering their crops, it would appear the mages have been collecting a harvest of their own. The Mages Guild is crowded with scrolls, relics, charms, and artifacts. Clearly some extensive trading between one town's guild to another's has been going on this fall.";
                else
                    raw = "You enter the Mages Guild, a world where nature holds little power. Outside the sun beats the land with heat, searing it clean. In here however, strange flora lives year round, competing for space next to the scrolls, potions, and other mystic apparati that crowds the Guild's shelves.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] SwampSpringMagesText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "Although the sky is gray the weather itself is quite hot. You can smell heat in the air, but that quickly changes as the door shuts behind you. The Mages Guild smells like old spices and chemicals. You can make out dozens of arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "You enter the Mages Guild from the gloomy spring day and are struck by the clutter. You know that wizardry is a mental art, and it is hard to believe the sorcerers behind this jumble of items and apparati are not either madmen or fools. You can definitely hear small living things move about in the piles.";
                else
                    raw = "The Mages Guild is not a place where the normal gods of Nature hold reign, but on such a bleak spring day, it does not look like they have power anywhere. Somehow the chamber is simultaneously too hot and too cold, wet with humidity in one spot, drier than the Khajiit desert in another. Even the mystical items on the shelves are juxtaposed between extremes, ancient scrolls next to modern measuring devices, this one filthy with neglect, that polished with care.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You shake the warm rain from your shoulders, conscious of the pooling water at your feet. As your eyes slowly adjust to the dim interior, you can make out arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "As thick as the stone walls of the Mages guild are, you can hear virtually every splatter of the warm spring rain outside. You are almost afraid to talk, for every echo of each footstep causes great clouds of dust to billow. And there are books and apparati in this room, you suspect, that should never be disturbed.";
                else
                    raw = "An open window in the Mages Guild lets in the smell of the spring storm, which mingles with the chemical stench that bubbles from an open cauldron. The chamber is filled with arcane objects, jugs, cloaks, scrolls, amulets, tomes, brooches, potions, robes, figurines, talismans.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "A cool breeze follows you into the Mages Guild, drying the slight sheen of sweat from your brow. The interior smells of spices and chemicals. As your eyes adjust to the dim light you can make out arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "You enter the Mages Guild, the smell of sulfur and rotting parchment replacing that of the %ct outside. Scrolls and potions, relics and artifacts vie for space on the bookshelves. You can hear small living things scuttling behind the volumes of alien encyclopedia.";
                else
                    raw = "The mystical energy in the Mages Guild runs through you like a charge of lightning. Outside nature is in her full vernal glory on this sunny day, but she has little power in here. From the stone walls to the scrolls and potions, the most mundane parts of this chamber seem like phantasmagoria.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] SwampSummerMagesText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter the Mages Guild, its gloomy interior not much different from the iron-gray, overcast sky outside. At least, you silently think, the sun isn't out. Around you are arcane implements and mystical apparati. You feel a slight tingling on your skin.";
                else if (variant == 1)
                    raw = "All the humidity and heat of the day are intensified in the Mages Guild. To add to the infernal atmosphere, tendrils of soot-filled smoke and the whimpers from some doomed experiment slip through the cracks of locked doors. Each book on the shelf looks like it is watching you.";
                else
                    raw = "You enter the Mages Guild, an amalgamation of primordial relics, moldering scrolls, dripping unguents, and smoking fluids. An occasional groan or minor explosion can be heard coming from the laboratories. At least it's not as hot and humid as outside.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You shake the hot rain from your shoulders as you enter the Mages Guild. The dry interior is a welcomed sight after standing in the downpour outside. Around you are arcane implements and mystical apparati. You feel a slight tingling on your skin.";
                else if (variant == 1)
                    raw = "The sound of the downpour is muffled by the thick walls and thicker velvet curtains. This is a place where the outside world is a distraction to be avoided. The smells, sights, and sounds here are unique to the field of mystical experimentation.";
                else
                    raw = "You enter the Mages Guild, avoiding the streams of water that create big patches of wet mud. The room is damp, but the thick stone walls keep it cool even in the dog days of summer. Strange devices and venerable tomes crowd across the dust and soot streaked shelves.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You wipe the sweat off your brow as you enter the Mages Guild. The cool shade of the interior is a welcomed sight after standing under the harsh summer sun. Around you are arcane implements and mystical apparati. You feel a slight tingling on your skin.";
                else if (variant == 1)
                    raw = "For a moment after leaving the bright summer sunshine, your eyes register only darkness in the gloomy Mages Guild. When they adjust, you can understand very little of what you see. Tomes of antiquated wizardry and obscure objects crowd the dusty shelves.";
                else
                    raw = "The door to the Mages Guild opens with a puff of dust that sticks to the sweat on your sunburned face. The smells of sulfurous potions burning and ancient scrolls of moldering parchment sting your nose. At least it is cool in here, you say to yourself.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] SwampWinterMagesText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "The gray sky has done little to warm your bones, which by now are cold indeed. You enter the Mages Guild, stamping your feet to increase the circulation and crowd close to a brazier that glows hot with fiery coals. As the warmth slowly seeps in, you notice arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "It is a perfect day for those who practice the shadowy arts: the Mages Guild comes most alive on chill, dark days. Cauldrons bubble on the fireplace, filling the room with translucent smoke that smells of animal fat and sulfur. The shelves are cluttered with mystical books and artifacts...";
                else
                    raw = "You know that you can only see half of what is in the Mages Guild. The titles of the books on the shelves seem to change depending on the angle you look. Invisible fingers run through the dust and over your face. From the corner of your eye, you can see demons grinning at you, but they vanish when you turn to look at them. As dark a day as it is outside, you want to do your business here and leave...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You shake the cold rain from your shoulders, conscious of the pooling water at your feet. As your eyes slowly adjust to the dim interior, you can make out arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "As thick as the stone walls of the Mages guild are, you can hear virtually every splatter of the rain outside. You are almost afraid to talk, for every echo of each footstep causes great clouds of dust to billow. And there are books and apparati in this room, you suspect, that should never be disturbed.";
                else
                    raw = "An open window in the Mages Guild lets in the smell of the storm, which mingles with the chemical stench that bubbles from an open cauldron. The chamber is filled with arcane objects, jugs, cloaks, scrolls, amulets, tomes, brooches, potions, robes, figurines, talismans.";
            }
            else if (weatherID == 3) // Snowy
            {
                if (variant == 0)
                    raw = "Chill winds swirl about you as you enter the Mages Guild and shut the door. Outside the winter's cold sets in without any signs of stopping. You stamp your feet and crowd close to a brazier that glows hot with fiery coals. As the warmth slowly seeps in, you notice arcane implements and mystical apparati. You think you feel a strange tingling over your skin.";
                else if (variant == 1)
                    raw = "The Mages Guild is so omnipresent that all the arcane relics within seem to be covered in a thin shroud. It is an almost soundless chamber, with only an occasional sigh from the pot of boiling liquid on the fire. Slowly warmth creeps back into your chilled body.";
                else
                    raw = "You enter the dim Mages Guild, warmed by a spell that brings life back into your chilled body almost immediately. Books, charms, scrolls, and other relics, older than human memory, are kept in pristine condition here by the same mystic energy.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "A cool breeze follows you into the Mages Guild, drying the slight sheen of sweat from your brow. The interior smells of spices and chemicals. As your eyes adjust to the dim light you can make out arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "As sunny as it was, you felt a cool chill outside and now, within the Mages Guild, you feel the cold more intensely. Dust lies on the shelves of books, potions, and relics like an early frost.";
                else
                    raw = "The melancholy Mages Guild is slightly warmer than the day outside but, without the sunlight, the effect is much the same. Many books and arcane instruments lie scattered about, but they fail to lend any vitality to the scene. It is a place of mystical study, not built for human comfort.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainFallMagesText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter the Mage's Guild, its gloomy interior not much different from the cold, overcast sky outside. At least, you silently thank, the interior is warm. You rub your hands together to bring back some feeling. Around you are arcane implements and mystical apparati. You feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "On a dark cool day like today, those who devote their lives to the mystical sciences are at their busiest. Outside, people look into the sky and view the coming winter with dread. All in the Mages Guild are as active with mystic energy as they might be on any day, including the books and relics that line the shelves.";
                else
                    raw = "You enter the Mages Guild, going from a cool gloomy outside to a cool gloomy inside. Because of the shadows, you cannot see all that the chamber contains. You suspect that even by the light of a bright day, the room has its secrets. Potions, relics, and books line the bookshelves shrouded by a fine dust.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You shake the cold rain from your shoulders as you enter the Mages Guild, stamping your feet to bring back some warmth.The dry interior is a welcomed sight after standing in the icy downpour outside. Around you are arcane implements and mystical apparati. You feel a slight tingling on your skin.";
                else if (variant == 1)
                    raw = "The Mages Guild is dry, but no more welcome that the cold autumn tempest outside. Strange and exotic potions, ancient scrolls, and arcane relics look down at you from the shelves as if they are predatory birds, watching for you to make a mistake. The dark energy in the room goes through you like a charge.";
                else
                    raw = "All along the walls of the Mages Guild, blackened with soot and neglect, are shelves of books and arcane apparati so cloaked in dust to be practically indistinguishable one from the other. You can hear the sound of the mountain winds outside, uncomfortably loud in the normally still little room.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "Behind you the fall sun sheds little heat upon the land, welcoming the icy fingers of winter yet another day. Around you are arcane implements and mystical apparati. You feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "While the local farmers have been gathering their crops, it would appear the mages have been collecting a harvest of their own. The Mages Guild is crowded withscrolls, relics, charms, and artifacts. Clearly some extensive trading between one town's guild to another's has been going on this fall.";
                else
                    raw = "You enter the Mages Guild, a world where nature holds little power. Outside the sun fights a losing battle against the cold. In here however, strange flora lives year round, competing for space next to the scrolls, potions, and other mystic apparati that crowds the Guild's shelves.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainSpringMagesText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "Although the sky is gray the weather itself is quite cold. You can smell dry chill in the air, but that quickly changes as the door shuts behind you. The Mages Guild smells like old spices and chemicals. You can make out dozens of arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "You enter the Mages Guild from the gloomy spring day and are struck by the clutter. You know that wizardry is a mental art, and it is hard to believe the sorcerers behind this jumble of items and apparati are not either madmen or fools. You can definitely hear small living things move about in the piles.";
                else
                    raw = "The Mages Guild is not a place where the normal gods of Nature hold reign, but on such a bleak spring day, it does not look like they have power anywhere. Somehow the chamber is simultaneously too hot and too cold, wet with humidity in one spot, drier than the Khajiit desert in another. Even the mystical items on the shelves are juxtaposed between extremes, ancient scrolls next to modern measuring devices, this one filthy with neglect, that polished with care.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You shake the cold rain from your shoulders, conscious of the pooling water at your feet. As your eyes slowly adjust to the dim interior, you can make out arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "As thick as the stone walls of the Mages guild are, you can hear virtually every splatter of the cold rain outside. You are almost afraid to talk, for every echo of each footstep causes great clouds of dust to billow. And there are books and apparati in this room, you suspect, that should never be disturbed.";
                else
                    raw = "An open window in the Mages Guild lets in the smell of the spring storm, which mingles with the chemical stench that bubbles from an open cauldron. The chamber is filled with arcane objects, jugs, cloaks, scrolls, amulets, tomes, brooches, potions, robes, figurines, talismans.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "A chill breeze follows you into the Mages Guild, drying the slight sheen of sweat from your brow. The interior smells of spices and chemicals. As your eyes adjust to the dim light you can make out arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "You enter the Mages Guild, the smell of sulfur and rotting parchment replacing that of new blossoms. Scrolls and potions, relics and artifacts vie for space on the bookshelves. You can hear small living things scuttling behind the volumes of alien encyclopedia.";
                else
                    raw = "The mystical energy in the Mages Guild runs through you like a charge of lightning. Outside nature is in her full arctic glory, but she has little power in here. From the stone walls to the scrolls and potions, the most mundane parts of this chamber seem like phantasmagoria.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainSummerMagesText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter the Mages Guild, its gloomy interior not much different from the iron-gray, overcast sky outside. At least, you silently think, the snow isn't falling. Around you are arcane implements and mystical apparati. You feel a slight tingling on your skin.";
                else if (variant == 1)
                    raw = "All the cold and chill of the day are intensified in the Mages Guild. To add to the frigid atmosphere, tendrils of soot-filled smoke and the whimpers from some doomed experiment slip through the cracks of locked doors. Each book on the shelf looks like it is watching you.";
                else
                    raw = "You enter the Mages Guild, an amalgamation of primordial relics, moldering scrolls, dripping unguents, and smoking fluids. An occasional groan or minor explosion can be heard coming from the laboratories. At least it's not as cold and windy as outside.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You shake the cold rain from your shoulders as you enter the Mages Guild. The dry interior is a welcomed sight after standing in the downpour outside. Around you are arcane implements and mystical apparati. You feel a slight tingling on your skin.";
                else if (variant == 1)
                    raw = "The sound of the brief summer rain is muffled by the thick walls and thicker velvet curtains. This is a place where the outside world is a distraction to be avoided. The smells, sights, and sounds here are unique to the field of mystical experimentation.";
                else
                    raw = "You enter the Mages Guild, avoiding the streams of water that run melted by the warm Guild air. The room is damp, but the thick stone walls keep it cool even in the dog days of summer. Strange devices and venerable tomes crowd across the dust and soot streaked shelves.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You enter the Mages Guild, happy to be out of the chill. The interior is a welcomed sight after standing in the harsh summer air. Around you are arcane implements and mystical apparati. You can feel a slight tingling on your skin.";
                else if (variant == 1)
                    raw = "For a moment after leaving the bright summer sunshine, your eyes register only darkness in the gloomy Mages Guild. When they adjust, you can understand very little of what you see. Tomes of antiquated wizardry and obscure objects crowd the dusty shelves.";
                else
                    raw = "The door to the Mages Guild opens with a puff of dust that sticks to the sweat on your sunburned face. The smells of sulfurous potions burning and ancient scrolls of moldering parchment sting your nose. At least it is warm in here, you say to yourself.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainWinterMagesText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "The gray sky has done little to warm your bones, which by now are almost frozen. You enter the Mages Guild, stamping your feet to increase the circulation and crowd close to a brazier that glows hot with fiery coals. As the warmth slowly seeps in, you notice arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "It is a perfect day for those who practice the shadowy arts: the Mages Guild comes most alive on cold, dark winter days. Cauldrons bubble on the fireplace, filling the room with translucent smoke that smells of animal fat and sulfur. The shelves are cluttered with mystical books and artifacts...";
                else
                    raw = "You know that you can only see half of what is in the Mages Guild. The titles of the books on the shelves seem to change depending on the angle you look. Invisible fingers run through the dust and over your face. From the corner of your eye, you can see demons grinning at you, but they vanish when you turn to look at them. As dark a winter day as it is outside, you want to do your business here and leave...";
            }
            else if (weatherID == 3) // Snowy
            {
                if (variant == 0)
                    raw = "Chill winds swirl about you as you enter the Mages Guild and shut the door. Outside the winter snow falls without any signs of stopping. You stamp your feet and crowd close to a brazier that glows hot with fiery coals. As the warmth slowly seeps in, you notice arcane implements and mystical apparati. You think you feel a strange tingling over your skin.";
                else if (variant == 1)
                    raw = "The Mages Guild is so omnipresent that all the arcane relics within seem to be covered in a thin shroud. It is an almost soundless chamber, with only an occasional sigh from the pot of boiling liquid on the fire. Slowly warmth creeps back into your chilled body.";
                else
                    raw = "You enter the dim Mages Guild, warmed by a spell that brings life back into your frozen body almost immediately. Books, charms, scrolls, and other relics, older than human memory, are kept in pristine condition here by the same mystic energy.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "The sun outside has done little to warm your bones, which are now almost frozen. You enter the Mages Guild, stamping your feet and crowding close to a brazier that glows hot with fiery coals. As the warmth slowly seeps in, you notice arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "Although the sun was out, you felt a winter's chill outside and now, within the Mages Guild, you feel the cold more intensely. Dust lies on the shelves of books, potions, and relics like an early frost. The stone walls of the chamber glisten with ice.";
                else
                    raw = "The melancholy Mages Guild is slightly warmer than the winter day outside but, without the sunlight, the effect is much the same. Many books and arcane instruments lie scattered about, but they fail to lend any vitality to the scene. It is a place of mystical study, not built for human comfort.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateFallMagesText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter the Mage's Guild, its gloomy interior not much different from the cold, overcast sky outside. At least, you silently thank, the interior is warm. You rub your hands together to bring back some feeling. Around you are arcane implements and mystical apparati. You feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "On a dark cool day like today, those who devote their lives to the mystical sciences are at their busiest. Outside, people look into the sky and view the coming winter with dread. All in the Mages Guild are as ctive with mystic energy as they might be on any day, including the books and relics that line the shelves.";
                else
                    raw = "You enter the Mages Guild, going from a cool gloomy outside to a cool gloomy inside. Because of the shadows, you cannot see all that the chamber contains. You suspect that even by the light of a bright day, the room has its secrets. Potions, relics, and books line the bookshelves shrouded by a fine dust.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You shake the cold rain from your shoulders as you enter the Mages Guild, stamping your feet to bring back some warmth. The dry interior is a welcomed sight after standing in the icy downpour outside. Around you are arcane implements and mystical apparati. You feel a slight tingling on your skin.";
                else if (variant == 1)
                    raw = "The Mages Guild is dry, but no more welcome that the cold autumn tempest outside. Strange and exotic potions, ancient scrolls, and arcane relics look down at you from the shelves as if they are predatory birds, watching for you to make a mistake. The dark energy in the room goes through you like a charge.";
                else
                    raw = "All along the walls of the Mages Guild, blackened with soot and neglect, are shelves of books and arcane apparati so cloaked in dust to be practically indistinguishable one from the other. You can hear the sound of the fall cloudburst outside, uncomfortably loud in the normally still little room.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "Behind you the fall sun sheds warmth upon the land, holding back the icy fingers of winter yet another day. Around you are arcane implements and mystical apparati. You feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "While the local farmers have been gathering their crops, it would appear the mages have been collecting a harvest of their own. The Mages Guild is crowded with scrolls, relics, charms, and artifacts. Clearly some extensive trading between one town's guild to another's has been going on this fall.";
                else
                    raw = "You enter the Mages Guild, a world where nature holds little power. Outside leaves are falling and plants are dying back into the ground, but in here, strange flora lives year round, competing for space next to the scrolls, potions, and other mystic apparati that crowds the Guild's shelves.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateSpringMagesText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "Although the sky is gray the weather itself is pleasant. You can smell rain in the air, but that quickly changes as the door shuts behind you. The Mages Guild smells like old spices and chemicals. You can make out dozens of arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "You enter the Mages Guild from the gloomy spring day and are struck by the clutter. You know that wizardry is a mental art, and it is hard to believe the sorcerers behind this jumble of items and apparati are not either madmen or fools. You can definitely hear small living things move about in the piles.";
                else
                    raw = "The Mages Guild is not a place where the normal gods of Nature hold reign, but on such a bleak spring day, it does not look like they have power anywhere. Somehow the chamber is simultaneously too hot and too cold, wet with humidity in one spot, drier than the Khajiit desert in another. Even the mystical items on the shelves are juxtaposed between extremes, ancient scrolls next to modern measuring devices, this one filthy with neglect, that polished with care.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You shake the warm rain from your shoulders, conscious of the pooling water at your feet. As your eyes slowly adjust to the dim interior, you can make out arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "As thick as the stone walls of the Mages guild are, you can hear virtually every splatter of the spring rain outside. You are almost afraid to talk, for every echo of each footstep causes great clouds of dust to billow. And there are books and apparati in this room, you suspect, that should never be disturbed.";
                else
                    raw = "An open window in the Mages Guild lets in the smell of the spring storm, which mingles with the chemical stench that bubbles from an open cauldron. The chamber is filled with arcane objects, jugs, cloaks, scrolls, amulets, tomes, brooches, potions, robes, figurines, talismans.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "A cool breeze follows you into the Mages Guild, drying the slight sheen of sweat from your brow. The interior smells of spices and chemicals. As your eyes adjust to the dim light you can make out arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "You enter the Mages Guild, the smell of sulfur and rotting parchment replacing that of new spring blossoms. Scrolls and potions, relics and artifacts vie for space on the bookshelves. You can hear small living things scuttling behind the volumes of alien encyclopedia.";
                else
                    raw = "The mystical energy in the Mages Guild runs through you like a charge of lightning. Outside nature is in her full vernal glory on this sunny day, but she has little power in here. From the stone walls to the scrolls and potions, the most mundane parts of this chamber seem like phantasmagoria.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateSummerMagesText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You enter the Mages Guild, its gloomy interior not much different from the iron-gray, overcast sky outside. At least, you silently think, the sun isn't out. Around you are arcane implements and mystical apparati. You feel a slight tingling on your skin.";
                else if (variant == 1)
                    raw = "All the humidity and heat of the day are intensified in the Mages Guild. To add to the infernal atmosphere, tendrils of soot-filled smoke and the whimpers from some doomed experiment slip through the cracks of locked doors. Each book on the shelf looks like it is watching you.";
                else
                    raw = "You enter the Mages Guild, an amalgamation of primordial relics, moldering scrolls, dripping unguents, and smoking fluids. An occasional groan or minor explosion can be heard coming from the laboratories. At least it's not as hot and humid as outside.";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You shake the rain from your shoulders as you enter the Mages Guild. The dry interior is a welcomed sight after standing in the downpour outside. Around you are arcane implements and mystical apparati. You feel a slight tingling on your skin.";
                else if (variant == 1)
                    raw = "The sound of the warm summer rain is muffled by the thick walls and thicker velvet curtains. This is a place where the outside world is a distraction to be avoided. The smells, sights, and sounds here are unique to the field of mystical experimentation.";
                else
                    raw = "You enter the Mages Guild, avoiding the puddles of rainwater leaking under the door. The room is damp, but the thick stone walls keep it cool even in the dog days of summer. Strange devices and venerable tomes crowd across the dust and soot streaked shelves.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You wipe the sweat off your brow as you enter the Mages Guild. The cool shade of the interior is a welcomed sight after standing under the harsh summer sun. Around you are arcane implements and mystical apparati. You feel a slight tingling on your skin.";
                else if (variant == 1)
                    raw = "For a moment after leaving the bright summer sunshine, your eyes register only darkness in the gloomy Mages Guild. When they adjust, you can understand very little of what you see. Tomes of antiquated wizardry and obscure objects crowd the dusty shelves.";
                else
                    raw = "The door to the Mages Guild opens with a puff of dust that sticks to the sweat on your sunburned face. The smells of sulfurous potions burning and ancient scrolls of moldering parchment sting your nose. At least it is cool in here, you say to yourself.";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateWinterMagesText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "The gray sky has done little to warm your bones, which by now are almost frozen. You enter the Mages Guild, stamping your feet to increase the circulation and crowd close to a brazier that glows hot with fiery coals. As the warmth slowly seeps in, you notice arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "It is a perfect day for those who practice the shadowy arts: the Mages Guild comes most alive on cold, dark winter days. Cauldrons bubble on the fireplace, filling the room with translucent smoke that smells of animal fat and sulfur. The shelves are cluttered with mystical books and artifacts...";
                else
                    raw = "You know that you can only see half of what is in the Mages Guild. The titles of the books on the shelves seem to change depending on the angle you look. Invisible fingers run through the dust and over your face. From the corner of your eye, you can see demons grinning at you, but they vanish when you turn to look at them. As dark a winter day as it is outside, you want to do your business here and leave...";
            }
            else if (weatherID == 3) // Snowy
            {
                if (variant == 0)
                    raw = "Snow and wind swirl about you as you enter the Mages Guild and shut the door. Outside the winter snow falls without any signs of stopping. You stamp your feet and crowd close to a brazier that glows hot with fiery coals. As the warmth slowly seeps in, you notice arcane implements and mystical apparati. You think you feel a strange tingling over your skin.";
                else if (variant == 1)
                    raw = "The white dust in the Mages Guild is not as deep as the snow outside, but it is so omnipresent that all the arcane relics within seem to be covered in a thin shroud. It is an almost soundless chamber, with only an occasional sigh from the pot of boiling liquid on the fire. Slowly warmth creeps back into your chilled body.";
                else
                    raw = "You enter the dim Mages Guild, warmed by a spell that dries the snow from your face and hair almost immediately. Books, charms, scrolls, and other relics, older than human memory, are kept in pristine condition here by the same mystic energy.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "The sun outside has done little to warm your bones, which are now almost frozen. You enter the Mages Guild, stamping your feet and crowding close to a brazier that glows hot with fiery coals. As the warmth slowly seeps in, you notice arcane implements and mystical apparati. You think you feel a strange tingling on your skin.";
                else if (variant == 1)
                    raw = "As sunny as it was, you felt a winter chill outside and now, within the Mages Guild, you feel the cold more intensely. Dust lies on the shelves of books, potions, and relics like an early frost. The stone walls of the chamber glisten with ice.";
                else
                    raw = "The melancholy Mages Guild is slightly warmer than the winter day outside but, without the sunlight, the effect is much the same. Many books and arcane instruments lie scattered about, but they fail to lend any vitality to the scene. It is a place of mystical study, not built for human comfort.";
            }
            return TextTokenFromRawString(raw);
        }

        #endregion

        #region Palace Text

        public static TextFile.Token[] HotFallPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and furs that decorate the area. Even though autumn's clouds outside have made everything seem dark and gray, you find that the interior is brightly lit and warm. Ahead of you waits the lord of " + CityName() + ".";
                else if (variant == 1)
                    raw = "You enter the audience chamber, shaking the heat of the gray autumn day from your skin. Being currently trade partners with its neighbor, " + RemoteTown() + ", the leading citizens of " + CityName() + " have met to discuss the resultant implications with their lord...";
                else
                    raw = "As you enter the audience chamber of the lord of " + CityName() + ", you feel the autumn heat from the outside is actually intensified within. The thick furs and darkly beautiful ornaments that decorate the walls make you feel as if you're being watched. The lord looks up from the conference he is having with the Council of Elders...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber shaking the hot rain from your shoulders. The first thing you notice is the many fine trappings and furniture that decorate the area. The desert air serves to quickly dry you from the fall shower outside. Ahead of you waits the lord of " + CityName() + "...";
                else if (variant == 1)
                    raw = "You had hoped that the chamber of the lord of " + CityName() + " would provide relief from the hot autumn rain outside, but it is as damp and chilly as a mausoleum. Others seeking audience wait with dripping hair and chattering teeth, as the lord talks with a representative from " + RemoteTown() + ", the city's neighbor that " + CityName() + " is trading with...";
                else
                    raw = "It has been an uncomfortably hot autumn in the city of " + CityName() + ", so the recent shower has actually improved the mood in the audience chamber of its lord. You know that " + RemoteTown() + ", " + CityName() + "'s temperamental neighbor, has only recently signed a trade deal with " + CityName() + ", but foreign diplomacy is not being discussed among the lord and the circle of counselors.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and furniture that decorate the area. Outside the golden fall sun lends its heat to the interior. Ahead of you waits the lord of " + CityName() + ".";
                else if (variant == 1)
                    raw = "It is the busiest season of the year in " + CityName() + ", and the palace's audience chamber is filled with area farmers discussing their harvests with their lord. Many linger in the room after their business is through, procrastinating the return to the burning field work. They seem to be talking mostly about " + CurrentRegion() + " being suddenly at peace with its nearest neighbor, and how this will change the trade throughout the region.";
                else
                    raw = "It is unusually hot for the autumn in " + CityName() + ", but the audience chamber reflects little of the sunshine outside...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] HotSpringPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0) // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You walk into the lord's audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The day has been overcast, iron gray clouds that hint of a spring storm. You find yourself dreading the sun's inevitable return. Ahead of you waits the lord of " + CityName() + ".";
                    else
                        raw = "You walk into the lord's audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The day has been overcast, iron gray clouds that hint of a spring storm. You find yourself welcoming the sun's return. Ahead of you waits the lord of " + CityName() + ".";
                }
                else if (variant == 1)
                    raw = "The government of " + CityName() + " is like the weather outside, hot and unforgiving. In the audience chamber no one even whispers their theories about the ulterior motives behind the city's recent deal with its neighbor, " + RemoteTown() + ".";
                else
                    raw = "You like the looks of the audience chamber the moment you enter. Beautiful music rings through the room and the walls hang with rich tapestries of spring scenes much nicer than the hot, gray day outside...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The hot spring shower has left you pleasantly refreshed, if a bit wet, but a hot breeze seems to be blowing through here and you feel yourself dry quickly...";
                else if (variant == 1)
                    raw = "For a desert-based city like " + CityName() + ", a good spring rain makes all the difference during the seeding. Everyone seems in a good mood, and the preparations for the traditional new year celebrations is the conversation on all lips...";
                else
                    raw = "Through an open window in the audience chamber, you can smell the spring rain and newly blossomed flowers out in the streets of " + CityName() + ". The floor is wet with the tread of many visitors...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The warm spring air wafts through an open window, bringing along with it the scent of newly blooming cacti, and fresh flowers.";
                else if (variant == 1)
                    raw = "It is hard to believe that anyone can think of politics on such a day, but people are lining up to be heard by the lord of " + CityName() + " while outside newly bloomed flowers perfume the air...";
                else
                    raw = "Inside the audience chamber of the lord of " + CityName() + ", the new sprouts of spring die young and the perfume of the fresh blossom turns to a sickly stench. The lord is rumored to have angered the city oracle who placed upon him a curse of poor health. The political change has been very noticeable in such a city where tradition is of highest importance...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] HotSummerPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the fine trappings and furniture that decorate the area. Even though the clouds outside have made everything seem dull and gray, you find that the interior is brightly lit and cheerful. Ahead of you waits the lord of " + CityName() + ".";
                else if (variant == 1)
                    raw = "You enter the audience chamber, finding the city elders in conference with their lord. The mood in the room reflects the weather outside; hot and dark with rain clouds. The elders continue to whisper nervously one to the other.";
                else
                    raw = "The audience chamber of " + CityName() + "'s lord is alive with activity. The lord and an elder discuss a discontented local group who wish " + CityName() + " to stop trading with " + RemoteTown() + ". He next turns his attention to a merchant whose shop is under suspicion of housing a Thieves' Guild's hideout...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the fine trappings and furniture that decorate the area. Outside you can still hear the raindrops as they fall, soaking everything in sight and creating a humid haze which causes your clothes to cling to you like a second skin. Ahead of you waits the lord of " + CityName() + ".";
                else if (variant == 1)
                    raw = "You enter the audience chamber, wiping the warm sweat of summer off of your face. At the center of the opulently decorated room, the lord is speaking with the city councilmen of " + CityName() + ". You overhear them mention " + RemoteTown() + ", the neighboring settlement over.";
                else
                    raw = "Like an unwanted dog, the dry heat from the hot summer has followed you to the audience chamber of " + CityName() + "'s lord. They whisper, but you think you hear one of the counselors remind the lord that they have a trade deal with " + RemoteTown() + ", the city's neighbor.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the fine trappings and furniture that decorate the area. A hot breeze wafts through the area, drying the sweat from your body and bringing an unpleasant sigh to your lips. You relish at the idea of a brief respite. Ahead of you waits the lord of " + CityName() + ".";
                else if (variant == 1)
                    raw = "Outside the sun batters relentlessly on the citizens of " + CityName() + ", but in the audience chamber of the city's lord, the shadows are deliverance. The rewards of power and wealth decorate the room. Obviously, trading with the city's neighbor has been profitable for the lord of " + CityName() + ". Perhaps you too can benefit from this situation...";
                else
                    raw = "You enter the audience chamber of " + CityName() + "'s lord. Despite the season, not a ray of sunshine has touched this room. You breath in the musty air and wipe the sweat from your brow as you wait for the lord to finish business with some messengers from " + RemoteTown() + ". The behavior between the lord and the messengers is peculiar, considering the two have been at peace for some time...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] HotWinterPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 4);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and furniture that decorate the area. The chilling temperature and iron gray clouds outside have made everything dull, though you find that the interior of this chamber is brightly lit and cheerful. Ahead of you waits the lord of " + CityName() + "...";
                else if (variant == 1)
                    raw = "Chilled by the cool air outside, you stumble into the audience chamber of " + CityName() + "'s lord, only to feel a colder chill that pierces your soul. The room itself is physically warm and pleasing to the eye, but you sense an undercurrent of tension...";
                else
                    raw = "You like the looks of the audience chamber the moment you enter. Beautiful music rings through the room and the walls hang with rich tapestries of desert scenes much nicer than the gray day outside...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The cool shower has left you pleasantly refreshed, if a bit cold, but a breeze seems to be blowing through here and you feel yourself dry quickly...";
                else if (variant == 1)
                    raw = "For a city like " + CityName() + ", rain showers makes all the difference. Everyone seems in a good mood, and the preparations for the traditional new year celebrations is the conversation on all lips...";
                else
                    raw = "Through an open window in the audience chamber, you can smell the rain in the streets of " + CityName() + ". The floor is wet with the tread of many visitors...";
            }
            else if (weatherID == 3) // Snowy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and warm furs that decorate the area. The chill in your bones quickly melts, and you feel your limbs begin to thaw in the warm hear of the brightly lit interior. You welcome the brief respite from the cold. Ahead of you waits the lord of " + CityName() + "...";
                else if (variant == 1)
                    raw = "The audience chamber is drier than the cold streets of " + CityName() + ", but not any warmer. You rub your hands together and watch your breath come like smoke from a chimney...";
                else
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and furs that decorate the area. Even though snow filled clouds outside have made everything seem dark and gray, you find that the interior is brightly lit and warm. Ahead of you waits the lord of " + CityName() + ".";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "The lord's audience chamber is alive with excitement. " + CityName() + " and representatives of " + RemoteTown() + ", its neighbor, are at business talks. Naturally, this has caused a lot of the internal stress within " + CityName() + ". The lord is remarkably calm, hearing each of the citizen's concerns about trade and commerce...";
                else if (variant == 1)
                    raw = "You enter the audience chamber of the lord of the city of " + CityName() + ", your flesh scorched by the sun. Representatives from " + RemoteTown() + ", which were meeting with the lord, leave the room as you enter. It appears things have not gone well, for the Council of Elders and the lord seem to be in a sour mood...";
                else if (variant == 2)
                    raw = "You enter the audience chamber of the lord of the city of " + CityName() + ", your flesh scorched by the sun. Representatives from __city_, which were meeting with the lord, leave the room as you enter. It appears things have gone well, for the Council of Elders and the lord seem to be in a good mood...";
                else
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and furniture that decorate the area. Even though the sun outside is bright and strong, this season's slightly colder temperatures have chilled your skin. You find that you welcome the well lit and warm interior. Ahead of you waits the lord of " + CityName() + "...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainFallPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and furs that decorate the area. Even though autumn's clouds outside have made everything seem dark and gray, you find that the interior is brightly lit and warm. Ahead of you waits the lord of " + CityName() + ".";
                else if (variant == 1)
                    raw = "You enter the audience chamber, shaking the chill of the gray autumn day from your skin. Being currently trade partners with its neighbor, " + RemoteTown() + ", the leading citizens of " + CityName() + " have met to discuss the resultant implications with their lord...";
                else
                    raw = "As you enter the audience chamber of the lord of " + CityName() + ", you feel the autumn chill from the outside is actually intensified within. The thick furs and darkly beautiful ornaments that decorate the walls make you feel as if you're being watched. The lord looks up from the conference he is having with the Council of Elders...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber shaking the cold rain from your shoulders. The first thing you notice is the many fine trappings and furniture that decorate the area. The mountain air serves to quickly chill you. Ahead of you waits the lord of " + CityName() + "...";
                else if (variant == 1)
                    raw = "You had hoped that the chamber of the lord of " + CityName() + " would provide relief from the cold autumn rain outside, but it is as damp and chilly as a mausoleum. Others seeking audience wait with dripping hair and chattering teeth, as the lord talks with a representative from " + RemoteTown() + ", the city's neighbor that " + CityName() + " is trading with...";
                else
                    raw = "It has been an uncomfortably cold autumn in the city of " + CityName() + ", so the recent shower has destroyed the mood in the audience chamber of its lord. You know that " + RemoteTown() + ", " + CityName() + "'s temperamental neighbor, has only recently signed a trade deal with " + CityName() + ", but foreign diplomacy is not being discussed among the lord and the circle of counselors.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and furniture that decorate the area. Outside the golden fall sun does nothing to lend its heat to the interior. Ahead of you waits the lord of " + CityName() + ".";
                else if (variant == 1)
                    raw = "It is the busiest season of the year in " + CityName() + ", and the palace's audience chamber is filled with area townsmen discussing the upcoming winter's weather with their lord. Many linger in the room after their business is through, procrastinating the return to the chill winds outside. They seem to be talking mostly about " + CurrentRegion() + " being suddenly at peace with its nearest neighbor, and how this will change the trade throughout the region.";
                else
                    raw = "It is unusually warm for the autumn in " + CityName() + ", but the audience chamber reflects little of the sun's heat outside...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainSpringPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0) // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You walk into the lord's audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The day has been overcast, iron gray clouds that hint of a spring storm. You find yourself dreading the sun's inevitable return. Ahead of you waits the lord of " + CityName() + ".";
                    else
                        raw = "You walk into the lord's audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The day has been overcast, iron gray clouds that hint of a spring storm. You find yourself welcoming the sun's return. Ahead of you waits the lord of " + CityName() + ".";
                }
                else if (variant == 1)
                    raw = "The government of " + CityName() + " is like the weather outside, cold and unforgiving. In the audience chamber no one even whispers their theories about the ulterior motives behind the city's recent deal with its neighbor, " + RemoteTown() + ".";
                else
                    raw = "You like the looks of the audience chamber the moment you enter. Beautiful music rings through the room and the walls hang with rich tapestries of spring scenes much nicer than the cold, gray day outside...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The cold spring shower has left you invigorated, but a bit wet. A warmer breeze seems to be blowing through here and you feel yourself dry quickly...";
                else if (variant == 1)
                    raw = "For a mountain-based city like " + CityName() + ", a good spring shower makes all the difference during this season. Everyone seems in a good mood, and the preparations for the traditional new year celebrations is the conversation on all lips...";
                else
                    raw = "Through an open window in the audience chamber, you can feel the rare spring warmth and newly blossomed flowers out in the streets of " + CityName() + ". The floor is wet with the tread of many visitors...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The cool spring air wafts through an open window, bringing along with it the scent of some fresh flowers.";
                else if (variant == 1)
                    raw = "It is hard to believe that anyone can think of politics on such a day, but people are lining up to be heard by the lord of " + CityName() + " while outside newly bloomed flowers perfume the air...";
                else
                    raw = "Inside the audience chamber of the lord of " + CityName() + ", the new sprouts of spring die young and the perfume of the fresh blossom turns to a sickly stench. The lord is rumored to have angered the city oracle who placed upon him a curse of poor health. The political change has been very noticeable in such a city where tradition is of highest importance...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainSummerPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the fine trappings and furniture that decorate the area. Even though the clouds outside have made everything seem dull and gray, you find that the interior is brightly lit and cheerful. Ahead of you waits the lord of " + CityName() + ".";
                else if (variant == 1)
                    raw = "You enter the audience chamber, finding the city elders in conference with their lord. The mood in the room reflects the weather outside; cold and dark with gray clouds. The elders continue to whisper nervously one to the other.";
                else
                    raw = "The audience chamber of " + CityName() + "'s lord is alive with activity. The lord and an elder discuss a discontented local group who wish " + CityName() + " to stop trading with " + RemoteTown() + ". He next turns his attention to a merchant whose shop is under suspicion of housing a Thieves' Guild's hideout...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the fine trappings and furniture that decorate the area. Outside you can still hear the raindrops as they fall, soaking everything in sight and creating a humid haze which causes your clothes to cling to you like a second skin. Ahead of you waits the lord of " + CityName() + ".";
                else if (variant == 1)
                    raw = "You enter the audience chamber, wiping the sweat from the warm air off of your face. At the center of the opulently decorated room, the lord is speaking with the city councilmen of " + CityName() + ". You overhear them mention " + RemoteTown() + ", the neighboring settlement over.";
                else
                    raw = "Like an unwanted dog, the dry chill from the cold summer has followed you to the audience chamber of " + CityName() + "'s lord. They whisper, but you think you hear one of the counselors remind the lord that they have a trade deal with " + RemoteTown() + ", the neighboring settlement.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the fine trappings and furniture that decorate the area. A cool breeze wafts through the area, chilling the sweat from your body and bringing an unpleasant sigh to your lips. You relish at the idea of a brief respite. Ahead of you waits the lord of " + CityName() + ".";
                else if (variant == 1)
                    raw = "Outside the wind batters relentlessly on the citizens of " + CityName() + ", but in the audience chamber of the city's lord, the shadows are deliverance. The rewards of power and wealth decorate the room. Obviously, trading with the city's neighbor has been profitable for the lord of " + CityName() + ". Perhaps you too can benefit from this situation...";
                else
                    raw = "You enter the audience chamber of " + CityName() + "'s lord. Despite the season, not a ray of sunshine has touched this room. You breath in the musty air and wipe the sweat from your brow as you wait for the lord to finish business with some messengers from " + RemoteTown() + ". The behavior between the lord and the messengers is peculiar, considering the two have been at peace for some time...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] MountainWinterPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 4);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and warm furs that decorate the area. The freezing temperature and iron gray clouds outside have made everything frozen and dull, though you find that the interior of this chamber is brightly lit and cheerful. Ahead of you waits the lord of " + CityName() + "...";
                else if (variant == 1)
                    raw = "Frozen to the core, you stumble into the audience chamber of " + CityName() + "'s lord, only to feel a chill that pierces your soul. The room itself is physically warm and pleasing to the eye, but you sense an undercurrent of tension...";
                else
                    raw = "You are admitted into the presence of the good lord of the city of " + CityName() + ". A thick cloak of wolf hide is gently wrapped around your shoulders, and as feeling returns to your skin, you feel that parts of your face were dangerously close to frostbite...";
            }
            else if (weatherID == 3) // Snowy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and warm furs that decorate the area. The chill in your bones quickly thaws, and you feel your limbs begin to heat in the warmth of the brightly lit interior. You welcome the brief respite from the cold. Ahead of you waits the lord of " + CityName() + "...";
                else if (variant == 1)
                    raw = "The audience chamber is drier than the cold streets of " + CityName() + ", but not any warmer. You rub your hands together and watch your breath come like smoke from a chimney...";
                else
                    raw = "Your eyes, squinting from the glare of the snow outside, grow accustomed to the gloom of the audience chamber.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "The lord's audience chamber is alive with excitement. " + CityName() + " and representatives of " + RemoteTown() + ", its neighbor to the %di, are at business talks. Naturally, this has caused a lot of the internal stress within " + CityName() + ". The lord is remarkably calm, hearing each of the citizen's concerns about trade and commerce...";
                else if (variant == 1)
                    raw = "You enter the audience chamber of the lord of the city of " + CityName() + ", your lips chapped from the cold and your flesh burned by the sun. Representatives from " + RemoteTown() + ", who were meeting with the lord, leave the room as you enter. It appears things have not gone well, for the Council of Elders and the lord seem to be in a sour mood...";
                else if (variant == 2)
                    raw = "You enter the audience chamber of the lord of the city of " + CityName() + ", your lips chapped from the cold and your flesh burned by the sun. Representatives from " + RemoteTown() + ", who were meeting with the lord, leave the room as you enter. It appears things have gone well, for the Council of Elders and the lord seem to be in a good mood...";
                else
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and warm furs that decorate the area. Even though the sun outside is strong, winter's hand has chilled you to the bone. You find that you welcome the well lit and warm interior. Ahead of you waits the lord of " + CityName() + "...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateFallPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and furs that decorate the area. Even though autumn's clouds outside have made everything seem cold and gray, you find that the interior is brightly lit and warm. Ahead of you waits the lord of " + CityName() + ".";
                else if (variant == 1)
                    raw = "You enter the audience chamber, shaking the chill of the gray autumn day from your skin. Being currently trade partners with its neighbor, " + RemoteTown() + ", the leading citizens of " + CityName() + " have met to discuss the resultant implications with their lord...";
                else
                    raw = "As you enter the audience chamber of the lord of " + CityName() + ", you feel the autumn chill from the outside is actually intensified within. The thick furs and darkly beautiful ornaments that decorate the walls make you feel as if you're being watched. The lord looks up from the conference he is having with the Council of Elders...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber shaking the cold rain from your shoulders. The first thing you notice is the many fine trappings and furniture that decorate the area. The warm air serves to quickly dry you from the fall shower outside. Ahead of you waits the lord of " + CityName() + "...";
                else if (variant == 1)
                    raw = "You had hoped that the chamber of the lord of " + CityName() + " would provide relief from the cold autumn rain outside, but it is as damp and chilly as a mausoleum. Others seeking audience wait with dripping hair and chattering teeth, as the lord talks with a representative from " + RemoteTown() + ", the city's neighbor that " + CityName() + " is trading with...";
                else
                    raw = "It has been an uncomfortably warm autumn in the city of " + CityName() + ", so the recent shower has actually improved the mood in the audience chamber of its lord. You know that " + RemoteTown() + ", " + CityName() + "'s temperamental neighbor, has only recently signed a trade deal with " + CityName() + ", but foreign diplomacy is not being discussed among the lord and the circle of counselors.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and furniture that decorate the area. Outside the golden fall sun lends its warmth, holding back winter's hand for a few moments more. Ahead of you waits the lord of " + CityName() + ".";
                else if (variant == 1)
                    raw = "It is the busiest season of the year in " + CityName() + ", and the palace's audience chamber is filled with area farmers discussing their harvests with their lord. Many linger in the room after their business is through, procrastinating the return to the burning field work. They seem to be talking mostly about " + CurrentRegion() + " being suddenly at peace with its nearest neighbor, and how this will change the trade throughout the region.";
                else
                    raw = "It is unusually warm for the autumn in " + CityName() + ", but the audience chamber reflects little of the sunshine outside...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateSpringPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The day has been overcast, iron gray clouds that hint of a spring storm. You find yourself welcoming the sun's return. Ahead of you waits the lord of " + CityName() + ".";
                else if (variant == 1)
                    raw = "The government of " + CityName() + " is like the weather outside, pleasantly temperate but dark with clouds threatening catastrophe. In the audience chamber no one even whispers their theories about the ulterior motives behind the city's recent deal with its neighbor, " + RemoteTown() + ".";
                else
                    raw = "You like the looks of the audience chamber the moment you enter. Beautiful music rings through the room and the walls hang with rich tapestries of spring scenes much nicer than the gray day outside...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The warm spring shower has left you pleasantly refreshed, if a bit wet, but a breeze seems to be blowing through here and you feel yourself dry quickly...";
                else if (variant == 1)
                    raw = "For an agriculture-based city like " + CityName() + ", a good spring rain makes all the difference during the seeding. Everyone seems in a good mood, and the preparations for the traditional new year celebrations is the conversation on all lips...";
                else
                    raw = "Through an open window in the audience chamber, you can smell the spring rain and newly blossomed flowers out in the streets of " + CityName() + ". The floor is wet with the tread of many visitors...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The warm spring air wafts through an open window, bringing along with it the scent of newly budding flowers and fresh green grass.";
                else if (variant == 1)
                    raw = "It is hard to believe that anyone can think of politics on such a day, but people are lining up to be heard by the lord of " + CityName() + " while outside newly bloomed flowers perfume the air...";
                else
                    raw = "Inside the audience chamber of the lord of " + CityName() + ", the new sprouts of spring die young and the perfume of the fresh blossom turns to a sickly stench. The lord is rumored to have angered the city oracle who placed upon him a curse of poor health. The political change has been very noticeable in such a city where tradition is of highest importance...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateSummerPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the fine trappings and furniture that decorate the area. Even though the clouds outside have made everything seem dull and gray, you find that the interior is brightly lit and cheerful. Ahead of you waits the lord of " + CityName() + ".";
                else if (variant == 1)
                    raw = "You enter the audience chamber, finding the city elders in conference with their lord. The mood in the room reflects the weather outside; hot and dark with rain clouds. The elders continue to whisper nervously one to the other.";
                else
                    raw = "The audience chamber of " + CityName() + "'s lord is alive with activity. The lord and an elder discuss a discontented local group who wish " + CityName() + " to stop trading with " + RemoteTown() + ". He next turns his attention to a merchant whose shop is under suspicion of housing a Thieves' Guild's hideout...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the fine trappings and furniture that decorate the area. Outside you can still hear the raindrops as they fall, soaking everything in sight and creating a humid haze which causes your clothes to cling to you like a second skin. Ahead of you waits the lord of " + CityName() + ".";
                else if (variant == 1)
                    raw = "You enter the audience chamber, wiping the warm water of the summer rain off of your face. At the center of the opulently decorated room, the lord is speaking with the city councilmen of " + CityName() + ". You overhear them mention " + RemoteTown() + ", the neighboring settlement over.";
                else
                    raw = "Like an unwanted dog, the steam from the hot summer drizzle has followed you to the audience chamber of " + CityName() + "'s lord. They whisper, but you think you hear one of the counselors remind the lord that they have a trade deal with " + RemoteTown() + ", the city's neighbor.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the fine trappings and furniture that decorate the area. A cool breeze wafts through the area, drying the sweat from your body and bringing a welcomed sigh to your lips. You relish in the brief respite. Ahead of you waits the lord of " + CityName() + ".";
                else if (variant == 1)
                    raw = "Outside the sun batters relentlessly on the citizens of " + CityName() + ", but in the audience chamber of the city's lord, the shadows are deliverance. The rewards of power and wealth decorate the room. Obviously, trading with the city's neighbor has been profitable for the lord of " + CityName() + ". Perhaps you too can benefit from this situation...";
                else
                    raw = "You enter the audience chamber of " + CityName() + "'s lord. Despite the season, not a warm ray of sunshine has touched this room. You breath in the musty air and wipe the sweat from your brow as you wait for the lord to finish business with some messengers from " + RemoteTown() + ". The behavior between the lord and the messengers is peculiar, considering the two have been at peace for some time...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] TemperateWinterPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 4);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and warm furs that decorate the area. The freezing temperature and iron gray clouds outside have made everything frozen and dull, though you find that the interior of this chamber is brightly lit and cheerful. Ahead of you waits the lord of " + CityName() + "...";
                else if (variant == 1)
                    raw = "Frozen to the core, you stumble into the audience chamber of " + CityName() + "'s lord, only to feel a chill that pierces your soul. The room itself is physically warm and pleasing to the eye, but you sense an undercurrent of tension...";
                else
                    raw = "You are admitted into the presence of the good lord of the city of " + CityName() + ". A thick cloak of wolf hide is gently wrapped around your shoulders, and as feeling returns to your skin, you feel that parts of your face were dangerously close to frostbite...";
            }
            else if (weatherID == 3) // Snowy
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and warm furs that decorate the area. You feel your frozen limbs begin to thaw in the warm heat of the brightly lit interior. You welcome the brief respite from the cold. Ahead of you waits the lord of " + CityName() + "...";
                else if (variant == 1)
                    raw = "The audience chamber is drier than the snowy streets of " + CityName() + ", but not any warmer. You rub your hands together and watch your breath come like smoke from a chimney...";
                else
                    raw = "Your eyes, squinting from the glare of the snow outside, grow accustomed to the gloom of the audience chamber.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into the lord's audience chamber, noticing the many fine trappings and warm furs that decorate the area. Even though the sun outside is strong, winter's hand has chilled you to the bone. You find that you welcome the brightly lit and warm interior. Ahead of you waits the lord of " + CityName() + "...";
                else if (variant == 1)
                    raw = "You enter the audience chamber of the lord of the city of " + CityName() + ", your lips chapped from the cold and your flesh burned by the sun. Representatives from " + RemoteTown() + ", who were meeting with the lord, leave the room as you enter. It appears things have not gone well, for the Council of Elders and the lord seem to be in a sour mood...";
                else if (variant == 2)
                    raw = "You enter the audience chamber of the lord of the city of " + CityName() + ", your lips chapped from the cold and your flesh burned by the sun. Representatives from " + RemoteTown() + ", who were meeting with the lord, leave the room as you enter. It appears things have gone well, for the Council of Elders and the lord seem to be in a good mood...";
                else
                    raw = "The lord's audience chamber is alive with excitement. " + CityName() + " and representatives of " + RemoteTown() + ", its neighbor, are at business talks. Naturally, this has caused a lot of the internal stress within " + CityName() + ". The lord is remarkably calm, hearing each of the citizen's concerns about trade and commerce...";
            }
            return TextTokenFromRawString(raw);
        }

        #endregion

        #region Daggerfall Castle Text

        public static TextFile.Token[] FallDFPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the many fine trappings and furs that decorate the area. Even though autumn's clouds outside have made everything seem cold and gray, you find that the interior is brightly lit and warm. Ahead of you waits the " + RegentTitle() + " of " + CityName() + ".";
                else if (variant == 1)
                    raw = "You enter the audience chamber, shaking the chill of the gray autumn day from your skin. Being currently trade partners with its neighbor on the other side of the Iliac Bay, Sentinel, the leading citizens of " + CityName() + " have met to discuss the resultant implications with their " + RegentTitle() + "...";
                else
                    raw = "As you enter the audience chamber of the " + RegentTitle() + " of " + CityName() + ", you feel the autumn chill from the outside is actually intensified within. The thick furs and darkly beautiful ornaments that decorate the walls make you feel as if you're being watched. The " + RegentTitle() + " looks up from the conference he is having with the Council of Elders...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber shaking the cold rain from your shoulders. The first thing you notice is the many fine trappings and furniture that decorate the area. The warm air serves to quickly dry you from the fall shower outside. Ahead of you waits the " + RegentTitle() + " of " + CityName() + "...";
                else if (variant == 1)
                    raw = "You had hoped that the chamber of the " + RegentTitle() + " of " + CityName() + " would provide relief from the cold autumn rain outside, but it is as damp and chilly as a mausoleum. Others seeking audience wait with dripping hair and chattering teeth, as the " + RegentTitle() + " talks with a representative from Anticlere, the region's neighbor to the east that " + CityName() + " is trading with...";
                else
                    raw = "It has been an uncomfortably warm autumn in the city of " + CityName() + ", so the recent shower has actually improved the mood in the audience chamber of its " + RegentTitle() + ". You know that Shalgora, " + CityName() + "'s temperamental neighbor, has only recently signed a trade deal with " + CityName() + ", but foreign diplomacy is not being discussed among the " + RegentTitle() + " and the circle of counselors.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the many fine trappings and furniture that decorate the area. Outside the golden fall sun lends its warmth, holding back winter's hand for a few moments more. Ahead of you waits the " + RegentTitle() + " of " + CityName() + ".";
                else if (variant == 1)
                    raw = "It is the busiest season of the year in " + CityName() + ", and the palace's audience chamber is filled with area farmers discussing their harvests with their " + RegentTitle() + ". Many linger in the room after their business is through, procrastinating the return to the burning field work. They seem to be talking mostly about " + CurrentRegion() + " being suddenly at peace with its nearest neighbor, and how this will change the trade throughout the region.";
                else
                    raw = "It is unusually warm for the autumn in " + CityName() + ", but " + RegentName() + "'s audience chamber reflects little of the sunshine outside...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] SpringDFPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0) // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You walk into " + RegentName() + "'s audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The day has been overcast, iron gray clouds that hint of a spring storm. You find yourself dreading the sun's inevitable return. Ahead of you waits the " + RegentTitle() + " of " + CityName() + ".";
                    else
                        raw = "You walk into " + RegentName() + "'s audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The day has been overcast, iron gray clouds that hint of a spring storm. You find yourself welcoming the sun's return. Ahead of you waits the " + RegentTitle() + " of " + CityName() + ".";
                }
                else if (variant == 1)
                    raw = "The government of " + CityName() + " is like the weather outside, pleasantly temperate but dark with clouds threatening catastrophe. In " + RegentName() + "'s audience chamber no one even whispers their theories about the ulterior motives behind the region's recent deal with its neighbor, Shalgora.";
                else
                    raw = "You like the looks of " + RegentName() + "'s audience chamber the moment you enter. Beautiful music rings through the room and the walls hang with rich tapestries of spring scenes much nicer than the gray day outside...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The warm spring shower has left you pleasantly refreshed, if a bit wet, but a breeze seems to be blowing through here and you feel yourself dry quickly...";
                else if (variant == 1)
                    raw = "For an agriculture-based city like " + CityName() + ", a good spring rain makes all the difference during the seeding. Everyone seems in a good mood, and the preparations for the traditional new year celebrations is the conversation on all lips...";
                else
                    raw = "Through an open window in " + RegentName() + "'s audience chamber, you can smell the spring rain and newly blossomed flowers out in the streets of " + CityName() + ". The floor is wet with the tread of many visitors...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The warm spring air wafts through an open window, bringing along with it the scent of newly budding flowers and fresh green grass.";
                else if (variant == 1)
                    raw = "It is hard to believe that anyone can think of politics on such a day, but people are lining up to be heard by the " + RegentTitle() + " of " + CityName() + " while outside newly bloomed flowers perfume the air...";
                else
                    raw = "Inside the audience chamber of the " + RegentTitle() + " of " + CityName() + ", the new sprouts of spring die young and the perfume of the fresh blossom turns to a sickly stench. " + RegentName() + " is rumored to have angered the city oracle who placed upon him a curse of poor health. The political change has been very noticeable in such a city where tradition is of highest importance...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] SummerDFPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the fine trappings and furniture that decorate the area. Even though the clouds outside have made everything seem dull and gray, you find that the interior is brightly lit and cheerful. Ahead of you waits the " + RegentTitle() + " of " + CityName() + ".";
                else if (variant == 1)
                    raw = "You enter the audience chamber, finding the city elders in conference with their " + RegentTitle() + ". The mood in the room reflects the weather outside; hot and dark with rain clouds. The elders continue to whisper nervously one to the other.";
                else
                    raw = "The audience chamber of " + RegentName() + " is alive with activity. The " + RegentTitle() + " and an elder discuss a discontented local group who wish " + CityName() + " to stop trading with Tulune. He next turns his attention to a merchant whose shop is under suspicion of housing a Thieves' Guild's hideout...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the fine trappings and furniture that decorate the area. Outside you can still hear the raindrops as they fall, soaking everything in sight and creating a humid haze which causes your clothes to cling to you like a second skin. Ahead of you waits the " + RegentTitle() + " of " + CityName() + ".";
                else if (variant == 1)
                    raw = "You enter the audience chamber, wiping the warm water of the summer rain off of your face. At the center of the opulently decorated room, the " + RegentTitle() + " is speaking with the city councilmen of " + CityName() + ". You overhear them mention " + RemoteTown() + ", their neighboring settlement over.";
                else
                    raw = "Like an unwanted dog, the steam from the hot summer drizzle has followed you to the audience chamber of " + RegentName() + ". They whisper, but you think you hear one of the counselors remind the " + RegentTitle() + " that they have a trade deal with Ilessan Hills, the region's neighbor to the north.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the fine trappings and furniture that decorate the area. A cool breeze wafts through the area, drying the sweat from your body and bringing a welcomed sigh to your lips. You relish in the brief respite. Ahead of you waits the " + RegentTitle() + " of " + CityName() + ".";
                else if (variant == 1)
                    raw = "Outside the sun batters relentlessly on the citizens of " + CityName() + ", but in " + RegentName() + "'s audience chamber, the shadows are deliverance. The rewards of power and wealth decorate the room. Obviously, trading with the his neighbors has been profitable for the " + RegentTitle() + " of " + CityName() + ". Perhaps you too can benefit from this situation...";
                else
                    raw = "You enter " + RegentName() + "'s audience chamber. Despite the season, not a warm ray of sunshine has touched this room. You breath in the musty air and wipe the sweat from your brow as you wait for the " + RegentTitle() + " to finish business with some messengers from Glenpoint. The behavior between the " + RegentTitle() + " and the messengers is peculiar, considering the two have been at peace for some time...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] WinterDFPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 4);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the many fine trappings and warm furs that decorate the area. The freezing temperature and iron gray clouds outside have made everything frozen and dull, though you find that the interior of this chamber is brightly lit and cheerful. Ahead of you waits " + RegentName() + ", " + RegentTitle() + " of " + CityName() + "...";
                else if (variant == 1)
                    raw = "Frozen to the core, you stumble into the audience chamber of " + RegentName() + ", " + RegentTitle() + " of " + CityName() + ", only to feel a chill that pierces your soul. The room itself is physically warm and pleasing to the eye, but you sense an undercurrent of tension...";
                else
                    raw = "You are admitted into the presence of the good " + RegentTitle() + " of " + CityName() + ", " + RegentName() + ". A thick cloak of wolf hide is gently wrapped around your shoulders, and as feeling returns to your skin, you feel that parts of your face were dangerously close to frostbite...";
            }
            else if (weatherID == 3) // Snowy
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the many fine trappings and warm furs that decorate the area. You feel your frozen limbs begin to thaw in the warm heat of the brightly lit interior. You welcome the brief respite from the cold. Ahead of you waits the " + RegentTitle() + " of " + CityName() + "...";
                else if (variant == 1)
                    raw = "The audience chamber of " + RegentName() + " is drier than the snowy streets of " + CityName() + ", but not any warmer. You rub your hands together and watch your breath come like smoke from a chimney...";
                else
                    raw = "Your eyes, squinting from the glare of the snow outside, grow accustomed to the gloom of " + RegentName() + "'s audience chamber.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = RegentName() + "'s audience chamber is alive with excitement. " + CityName() + " and representatives of " + RemoteTown() + ", its neighbor, are at business talks. Naturally, this has caused a lot of the internal stress within " + CityName() + ". The " + RegentTitle() + " is remarkably calm, hearing each of the citizen's concerns about trade and commerce...";
                else if (variant == 1)
                    raw = "You enter the audience chamber of " + RegentName() + " of " + CityName() + ", your lips chapped from the cold and your flesh scorched by the sun. Representatives from Sentinel, who were meeting with the " + RegentTitle() + ", leave the room as you enter. It appears things have not gone well, for the Council of Elders and the " + RegentTitle() + " seem to be in a sour mood...";
                else if (variant == 2)
                    raw = "You enter the audience chamber of " + RegentName() + " of " + CityName() + ", your lips chapped from the cold and your flesh scorched by the sun. Representatives from Wayrest, who were meeting with the " + RegentTitle() + ", leave the room as you enter. It appears things have gone well, for the Council of Elders and the " + RegentTitle() + " seem to be in a good mood...";
                else
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the many fine trappings and warm furs that decorate the area. Even though the sun outside is strong, winter's hand has chilled you to the bone. You find that you welcome the brightly lit and warm interior. Ahead of you waits " + RegentName() + ", " + RegentTitle() + " of " + CityName() + "...";
            }
            return TextTokenFromRawString(raw);
        }

        #endregion

        #region Sentinel Castle Text

        public static TextFile.Token[] FallSentPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into the " + RegentTitle() + "'s audience chamber, noticing the many fine trappings and furs that decorate the area. Even though autumn's clouds outside have made everything seem dark and gray, you find that the interior is brightly lit and warm. Somewhere within waits the " + RegentTitle() + " of " + CityName() + ".";
                else if (variant == 1)
                    raw = "You enter the audience chamber, shaking the heat of the gray autumn day from your skin. Being currently trade partners with its neighbor to the west, Myrkwasa, the leading citizens of " + CityName() + " have met to discuss the resultant implications with their " + RegentTitle() + "...";
                else
                    raw = "As you enter the audience chamber of the " + RegentTitle() + " of " + CityName() + ", you feel the autumn heat from the outside is actually intensified within. The thick furs and darkly beautiful ornaments that decorate the walls make you feel as if you're being watched. The " + RegentTitle() + " looks up from the conference she is having with the Council of Elders...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into the " + RegentTitle() + "'s audience chamber shaking the hot rain from your shoulders. The first thing you notice is the many fine trappings and furniture that decorate the area. The desert air serves to quickly dry you from the fall shower outside. Somewhere within waits the " + RegentTitle() + " of " + CityName() + "...";
                else if (variant == 1)
                    raw = "You had hoped that " + RegentName() + "'s chamber would provide relief from the hot autumn rain outside, but it is as damp and chilly as a mausoleum. Others seeking audience wait with dripping hair and chattering teeth, as the " + RegentTitle() + " talks with a representative from the Alik'r Desert, her neighbor to the south that " + CityName() + " is trading with...";
                else
                    raw = "It has been an uncomfortably hot autumn in the city of " + CityName() + ", so the recent shower has actually improved the mood in the audience chamber of its " + RegentTitle() + ". You know that Ayasofya, " + CityName() + "'s temperamental neighbor, has only recently signed a trade deal with " + CityName() + ", but foreign diplomacy is not being discussed among the " + RegentTitle() + " and the circle of counselors.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into the " + RegentTitle() + "'s audience chamber, noticing the many fine trappings and furniture that decorate the area. Outside the golden fall sun lends its heat to the interior. Ahead of you waits the " + RegentTitle() + " of " + CityName() + ".";
                else if (variant == 1)
                    raw = "It is the busiest season of the year in " + CityName() + ", and the palace's audience chamber is filled with area farmers discussing their harvests with their " + RegentTitle() + ". Many linger in the room after their business is through, procrastinating the return to the burning field work. They seem to be talking mostly about " + CurrentRegion() + " being suddenly at peace with its nearest neighbor, and how this will change the trade throughout the region.";
                else
                    raw = "It is unusually hot for the autumn in " + CityName() + ", but the audience chamber reflects little of the sunshine outside...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] SpringSentPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into the " + RegentTitle() + "'s audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The day has been overcast, iron gray clouds that hint of a spring storm. You find yourself welcoming the sun's return. Somewhere within waits the " + RegentTitle() + " of " + CityName() + ".";
                else if (variant == 1)
                    raw = "The government of " + CityName() + " is like the weather outside, hot and unforgiving. In the audience chamber no one even whispers their theories about the ulterior motives behind the city's recent deal with its neighbor, Antiphyllos.";
                else
                    raw = "You like the looks of the audience chamber the moment you enter. Beautiful music rings through the room and the walls hang with rich tapestries of spring scenes much nicer than the hot, gray day outside...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into the " + RegentTitle() + "'s audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The hot spring shower has left you pleasantly refreshed, if a bit wet, but a hot breeze seems to be blowing through here and you feel yourself dry quickly...";
                else if (variant == 1)
                    raw = "For a desert-based city like " + CityName() + ", a good spring rain makes all the difference during the seeding. Everyone seems in a good mood, and the preparations for the traditional new year celebrations is the conversation on all lips...";
                else
                    raw = "Through an open window in the audience chamber, you can smell the spring rain and newly blossomed flowers out in the streets of " + CityName() + ". The floor is wet with the tread of many visitors...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into the " + RegentTitle() + "'s audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The warm spring air wafts through an open window, bringing along with it the scent of newly blooming cacti, and fresh flowers.";
                else if (variant == 1)
                    raw = "It is hard to believe that anyone can think of politics on such a day, but people are lining up to be heard by the " + RegentTitle() + " of " + CityName() + " while outside newly bloomed flowers perfume the air...";
                else
                    raw = "Inside the audience chamber of the " + RegentTitle() + " of " + CityName() + ", the new sprouts of spring die young and the perfume of the fresh blossom turns to a sickly stench. " + RegentName() + " is rumored to have angered the city oracle who placed upon her a curse of poor health. The political change has been very noticeable in such a city where tradition is of highest importance...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] SummerSentPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into the " + RegentTitle() + "'s audience chamber, noticing the fine trappings and furniture that decorate the area. Even though the clouds outside have made everything seem dull and gray, you find that the interior is brightly lit and cheerful. Ahead of you waits the " + RegentTitle() + " of " + CityName() + ".";
                else if (variant == 1)
                    raw = "You enter the audience chamber, finding the city elders in conference with their " + RegentTitle() + ". The mood in the room reflects the weather outside; hot and dark with rain clouds. The elders continue to whisper nervously one to the other.";
                else
                    raw = RegentName() + "'s audience chamber is alive with activity. The " + RegentTitle() + " and an elder discuss a discontented local group who wish " + CityName() + " to stop trading with Tigonus. She next turns her attention to a merchant whose shop is under suspicion of housing a Thieves' Guild's hideout...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into the " + RegentTitle() + "'s audience chamber, noticing the fine trappings and furniture that decorate the area. Outside you can still hear the raindrops as they fall, soaking everything in sight and creating a humid haze which causes your clothes to cling to you like a second skin. Somewhere within waits the " + RegentTitle() + " of " + CityName() + ".";
                else if (variant == 1)
                    raw = "You enter the audience chamber, wiping the warm sweat of summer off of your face. At the center of the opulently decorated room, the " + RegentTitle() + " is speaking with the city councilmen of " + CityName() + ". You overhear them mention " + RemoteTown() + ", the neighboring settlement over.";
                else
                    raw = "Like an unwanted dog, the dry heat from the hot summer has followed you to the audience chamber of " + CityName() + "'s " + RegentTitle() + ". They whisper, but you think you hear one of the counselors remind the " + RegentTitle() + " that they have a trade deal with " + RemoteTown() + ", the city's neighbor.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into the " + RegentTitle() + "'s audience chamber, noticing the fine trappings and furniture that decorate the area. A hot breeze wafts through the area, drying the sweat from your body and bringing an unpleasant sigh to your lips. You relish at the idea of a brief respite. Somewhere within waits the " + RegentTitle() + " of " + CityName() + ".";
                else if (variant == 1)
                    raw = "Outside the sun batters relentlessly on the citizens of " + CityName() + ", but in " + RegentName() + "'s audience chamber, the shadows are deliverance. The rewards of power and wealth decorate the room. Obviously, trading with her neighbor has been profitable for the " + RegentTitle() + " of " + CityName() + ". Perhaps you too can benefit from this situation...";
                else
                    raw = "You enter " + RegentName() + "'s audience chamber. Despite the season, not a ray of sunshine has touched this room. You breath in the musty air and wipe the sweat from your brow as you wait for the " + RegentTitle() + " to finish business with some messengers from Pothago. The behavior between the " + RegentTitle() + " and the messengers is peculiar, considering the two have been at peace for some time...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] WinterSentPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 4);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into the " + RegentTitle() + "'s audience chamber, noticing the many fine trappings and furniture that decorate the area. The chilling temperature and iron gray clouds outside have made everything dull, though you find that the interior of this chamber is brightly lit and cheerful. Somewhere within waits " + RegentTitle() + " of " + CityName() + "...";
                else if (variant == 1)
                    raw = "Chilled by the cool air outside, you stumble into " + RegentName() + "'s audience chamber, only to feel a colder chill that pierces your soul. The room itself is physically warm and pleasing to the eye, but you sense an undercurrent of tension...";
                else
                    raw = "You like the looks of the audience chamber the moment you enter. Beautiful music rings through the room and the walls hang with rich tapestries of desert scenes much nicer than the gray day outside...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into the " + RegentTitle() + "'s audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The cool shower has left you pleasantly refreshed, if a bit cold, but a breeze seems to be blowing through here and you feel yourself dry quickly...";
                else if (variant == 1)
                    raw = "For a desert city like " + CityName() + ", rain showers makes all the difference. Everyone seems in a good mood, and the preparations for the traditional new year celebrations is the conversation on all lips...";
                else
                    raw = "Through an open window in the audience chamber, you can smell the rain in the streets of " + CityName() + ". The floor is wet with the tread of many visitors...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "The " + RegentTitle() + "'s audience chamber is alive with excitement. " + CityName() + " and representatives of the Ayasofya, its neighbor to the east, are at business talks. Naturally, this has caused a lot of the internal stress within " + CityName() + ". The " + RegentTitle() + " is remarkably calm, hearing each of the citizen's concerns about trade and commerce...";
                else if (variant == 1)
                    raw = "You enter " + RegentName() + "'s audience chamber, your flesh scorched by the sun. Representatives from Daggerfall, which were meeting with the " + RegentTitle() + ", leave the room as you enter. It appears things have not gone well, for the Council of Elders and the " + RegentTitle() + " seem to be in a sour mood...";
                else if (variant == 2)
                    raw = "You enter " + RegentName() + "'s audience chamber, your flesh scorched by the sun. Representatives from Wayrest, which were meeting with the " + RegentTitle() + ", leave the room as you enter. It appears things have gone well, for the Council of Elders and the " + RegentTitle() + " seem to be in a good mood...";
                else
                    raw = "You walk into the " + RegentTitle() + "'s audience chamber, noticing the many fine trappings and furniture that decorate the area. Even though the sun outside is bright and strong, this season's slightly colder temperatures have chilled your skin. You find that you welcome the well lit and warm interior. Somewhere within waits " + RegentName() + ", " + RegentTitle() + " of " + CityName() + "...";
            }
            return TextTokenFromRawString(raw);
        }

        #endregion

        #region Wayrest Castle Text

        public static TextFile.Token[] FallWayPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the many fine trappings and furs that decorate the area. Even though autumn's clouds outside have made everything seem cold and gray, you find that the interior is brightly lit and warm. Ahead of you waits the " + RegentTitle() + " of " + CityName() + ".";
                else if (variant == 1)
                    raw = "You enter the audience chamber, shaking the chill of the gray autumn day from your skin. Being currently trade partners with its neighbor on the other side of the Iliac Bay, Daggerfall, the leading citizens of " + CityName() + " have met to discuss the resultant implications with their " + RegentTitle() + "...";
                else
                    raw = "As you enter the audience chamber of the " + RegentTitle() + " of " + CityName() + ", you feel the autumn chill from the outside is actually intensified within. The thick furs and darkly beautiful ornaments that decorate the walls make you feel as if you're being watched. The " + RegentTitle() + " looks up from the conference he is having with the Council of Elders...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber shaking the cold rain from your shoulders. The first thing you notice is the many fine trappings and furniture that decorate the area. The warm air serves to quickly dry you from the fall shower outside. Ahead of you waits the " + RegentTitle() + " of " + CityName() + "...";
                else if (variant == 1)
                    raw = "You had hoped that the chamber of the " + RegentTitle() + " of " + CityName() + " would provide relief from the cold autumn rain outside, but it is as damp and chilly as a mausoleum. Others seeking audience wait with dripping hair and chattering teeth, as the " + RegentTitle() + " talks with a representative from Wrothgaria, the region's neighbor to the north that " + CityName() + " is trading with...";
                else
                    raw = "It has been an uncomfortably warm autumn in the city of " + CityName() + ", so the recent shower has actually improved the mood in the audience chamber of its " + RegentTitle() + ". You know that Satakalaam, " + CityName() + "'s temperamental neighbor, has only recently signed a trade deal with " + CityName() + ", but foreign diplomacy is not being discussed among the " + RegentTitle() + " and the circle of counselors.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the many fine trappings and furniture that decorate the area. Outside the golden fall sun lends its warmth, holding back winter's hand for a few moments more. Ahead of you waits the " + RegentTitle() + " of " + CityName() + ".";
                else if (variant == 1)
                    raw = "It is the busiest season of the year in " + CityName() + ", and the palace's audience chamber is filled with area farmers discussing their harvests with their " + RegentTitle() + ". Many linger in the room after their business is through, procrastinating the return to the burning field work. They seem to be talking mostly about " + CurrentRegion() + " being suddenly at peace with its nearest neighbor, and how this will change the trade throughout the region.";
                else
                    raw = "It is unusually warm for the autumn in " + CityName() + ", but " + RegentName() + "'s audience chamber reflects little of the sunshine outside...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] SpringWayPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0) // Add Vampire/Damage From Sun Variant (Done)
                {
                    if (GameManager.Instance.PlayerEffectManager.HasVampirism() || GameManager.Instance.PlayerEntity.Career.DamageFromSunlight)
                        raw = "You walk into " + RegentName() + "'s audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The day has been overcast, iron gray clouds that hint of a spring storm. You find yourself dreading the sun's inevitable return. Ahead of you waits the " + RegentTitle() + " of " + CityName() + ".";
                    else
                        raw = "You walk into " + RegentName() + "'s audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The day has been overcast, iron gray clouds that hint of a spring storm. You find yourself welcoming the sun's return. Ahead of you waits the " + RegentTitle() + " of " + CityName() + ".";
                }
                else if (variant == 1)
                    raw = "The government of " + CityName() + " is like the weather outside, pleasantly temperate but dark with clouds threatening catastrophe. In " + RegentName() + "'s audience chamber no one even whispers their theories about the ulterior motives behind the region's recent deal with its neighbor, Gavaudon.";
                else
                    raw = "You like the looks of " + RegentName() + "'s audience chamber the moment you enter. Beautiful music rings through the room and the walls hang with rich tapestries of spring scenes much nicer than the gray day outside...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The warm spring shower has left you pleasantly refreshed, if a bit wet, but a breeze seems to be blowing through here and you feel yourself dry quickly...";
                else if (variant == 1)
                    raw = "For an agriculture-based city like " + CityName() + ", a good spring rain makes all the difference during the seeding. Everyone seems in a good mood, and the preparations for the traditional new year celebrations is the conversation on all lips...";
                else
                    raw = "Through an open window in " + RegentName() + "'s audience chamber, you can smell the spring rain and newly blossomed flowers out in the streets of " + CityName() + ". The floor is wet with the tread of many visitors...";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the many fine trappings and expensive furniture that decorate the area. The warm spring air wafts through an open window, bringing along with it the scent of newly budding flowers and fresh green grass.";
                else if (variant == 1)
                    raw = "It is hard to believe that anyone can think of politics on such a day, but people are lining up to be heard by the " + RegentTitle() + " of " + CityName() + " while outside newly bloomed flowers perfume the air...";
                else
                    raw = "Inside the audience chamber of the " + RegentTitle() + " of " + CityName() + ", the new sprouts of spring die young and the perfume of the fresh blossom turns to a sickly stench. " + RegentName() + " is rumored to have angered the city oracle who placed upon him a curse of poor health. The political change has been very noticeable in such a city where tradition is of highest importance...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] SummerWayPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 3);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the fine trappings and furniture that decorate the area. Even though the clouds outside have made everything seem dull and gray, you find that the interior is brightly lit and cheerful. Ahead of you waits the " + RegentTitle() + " of " + CityName() + ".";
                else if (variant == 1)
                    raw = "You enter the audience chamber, finding the city elders in conference with their " + RegentTitle() + ". The mood in the room reflects the weather outside; hot and dark with rain clouds. The elders continue to whisper nervously one to the other.";
                else
                    raw = "The audience chamber of " + RegentName() + " is alive with activity. The " + RegentTitle() + " and an elder discuss a discontented local group who wish " + CityName() + " to stop trading with Kambria. He next turns his attention to a merchant whose shop is under suspicion of housing a Thieves' Guild's hideout...";
            }
            else if (weatherID == 2) // Rainy
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the fine trappings and furniture that decorate the area. Outside you can still hear the raindrops as they fall, soaking everything in sight and creating a humid haze which causes your clothes to cling to you like a second skin. Ahead of you waits the " + RegentTitle() + " of " + CityName() + ".";
                else if (variant == 1)
                    raw = "You enter the audience chamber, wiping the warm water of the summer rain off of your face. At the center of the opulently decorated room, the " + RegentTitle() + " is speaking with the city councilmen of " + CityName() + ". You overhear them mention " + RemoteTown() + ", the neighboring settlement over.";
                else
                    raw = "Like an unwanted dog, the steam from the hot summer drizzle has followed you to the audience chamber of " + RegentName() + ". They whisper, but you think you hear one of the counselors remind the " + RegentTitle() + " that they have a trade deal with Mournoth, the region's neighbor to the south.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the fine trappings and furniture that decorate the area. A cool breeze wafts through the area, drying the sweat from your body and bringing a welcomed sigh to your lips. You relish in the brief respite. Ahead of you waits the " + RegentTitle() + " of " + CityName() + ".";
                else if (variant == 1)
                    raw = "Outside the sun batters relentlessly on the citizens of " + CityName() + ", but in " + RegentName() + "'s audience chamber, the shadows are deliverance. The rewards of power and wealth decorate the room. Obviously, trading with the his neighbors has been profitable for the " + RegentTitle() + " of " + CityName() + ". Perhaps you too can benefit from this situation...";
                else
                    raw = "You enter " + RegentName() + "'s audience chamber. Despite the season, not a warm ray of sunshine has touched this room. You breath in the musty air and wipe the sweat from your brow as you wait for the " + RegentTitle() + " to finish business with some messengers from Menevia. The behavior between the " + RegentTitle() + " and the messengers is peculiar, considering the two have been at peace for some time...";
            }
            return TextTokenFromRawString(raw);
        }

        public static TextFile.Token[] WinterWayPalaceText(int weatherID)
        {
            int variant = UnityEngine.Random.Range(0, 4);
            string raw = "";

            if (weatherID == 1) // Cloudy
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the many fine trappings and warm furs that decorate the area. The freezing temperature and iron gray clouds outside have made everything frozen and dull, though you find that the interior of this chamber is brightly lit and cheerful. Ahead of you waits " + RegentName() + ", " + RegentTitle() + " of " + CityName() + "...";
                else if (variant == 1)
                    raw = "Frozen to the core, you stumble into the audience chamber of " + RegentName() + ", " + RegentTitle() + " of " + CityName() + ", only to feel a chill that pierces your soul. The room itself is physically warm and pleasing to the eye, but you sense an undercurrent of tension...";
                else
                    raw = "You are admitted into the presence of the good " + RegentTitle() + " of " + CityName() + ", " + RegentName() + ". A thick cloak of wolf hide is gently wrapped around your shoulders, and as feeling returns to your skin, you feel that parts of your face were dangerously close to frostbite...";
            }
            else if (weatherID == 3) // Snowy
            {
                if (variant == 0)
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the many fine trappings and warm furs that decorate the area. You feel your frozen limbs begin to thaw in the warm heat of the brightly lit interior. You welcome the brief respite from the cold. Ahead of you waits the " + RegentTitle() + " of " + CityName() + "...";
                else if (variant == 1)
                    raw = "The audience chamber of " + RegentName() + " is drier than the snowy streets of " + CityName() + ", but not any warmer. You rub your hands together and watch your breath come like smoke from a chimney...";
                else
                    raw = "Your eyes, squinting from the glare of the snow outside, grow accustomed to the gloom of " + RegentName() + "'s audience chamber.";
            }
            else // Sunny or anything else
            {
                if (variant == 0)
                    raw = RegentName() + "'s audience chamber is alive with excitement. " + CityName() + " and representatives of " + RemoteTown() + ", its neighbor, are at business talks. Naturally, this has caused a lot of the internal stress within " + CityName() + ". The " + RegentTitle() + " is remarkably calm, hearing each of the citizen's concerns about trade and commerce...";
                else if (variant == 1)
                    raw = "You enter the audience chamber of " + RegentName() + " of " + CityName() + ", your lips chapped from the cold and your flesh scorched by the sun. Representatives from Sentinel, who were meeting with the " + RegentTitle() + ", leave the room as you enter. It appears things have not gone well, for the Council of Elders and the " + RegentTitle() + " seem to be in a sour mood...";
                else if (variant == 2)
                    raw = "You enter the audience chamber of " + RegentName() + " of " + CityName() + ", your lips chapped from the cold and your flesh scorched by the sun. Representatives from Daggerfall, who were meeting with the " + RegentTitle() + ", leave the room as you enter. It appears things have gone well, for the Council of Elders and the " + RegentTitle() + " seem to be in a good mood...";
                else
                    raw = "You walk into " + RegentName() + "'s audience chamber, noticing the many fine trappings and warm furs that decorate the area. Even though the sun outside is strong, winter's hand has chilled you to the bone. You find that you welcome the brightly lit and warm interior. Ahead of you waits " + RegentName() + ", " + RegentTitle() + " of " + CityName() + "...";
            }
            return TextTokenFromRawString(raw);
        }

        #endregion
    }
}