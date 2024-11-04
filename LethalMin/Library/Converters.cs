//TODO: Make this class convert LethalMinLibrary PikminTypes to LethalMin types.

using System.Linq;
using LethalMinLibrary;

namespace LethalMin.Library
{
    public static class TypeConverter
    {

    }
    public static class EnumConverter
    {
        public static HazardType Convert_Lib_HazardToLmHazard(LibHazardType libHazardType)
        {
            switch (libHazardType)
            {
                case LibHazardType.Lethal:
                    return HazardType.Lethal;
                case LibHazardType.Poison:
                    return HazardType.Poison;
                case LibHazardType.Fire:
                    return HazardType.Fire;
                case LibHazardType.Electric:
                    return HazardType.Electric;
                case LibHazardType.Water:
                    return HazardType.Water;
                case LibHazardType.Exsplosive:
                    return HazardType.Exsplosive;
                case LibHazardType.Crush:
                    return HazardType.Crush;
                default:
                    return HazardType.Lethal;
            }
        }
        public static HazardType[] Convert_Lib_HazardToLmHazard(LibHazardType[] libHazardTypes)
        {
            return libHazardTypes.Select(libHazardType => Convert_Lib_HazardToLmHazard(libHazardType)).ToArray();
        }
        public static LibHazardType Convert_LethalMin_HazardToLibHazard(HazardType HazardType)
        {
            switch (HazardType)
            {
                case HazardType.Lethal:
                    return LibHazardType.Lethal;
                case HazardType.Poison:
                    return LibHazardType.Poison;
                case HazardType.Fire:
                    return LibHazardType.Fire;
                case HazardType.Electric:
                    return LibHazardType.Electric;
                case HazardType.Water:
                    return LibHazardType.Water;
                case HazardType.Exsplosive:
                    return LibHazardType.Exsplosive;
                case HazardType.Crush:
                    return LibHazardType.Crush;
                default:
                    return LibHazardType.Lethal;
            }
        }        
        
        public static LibHazardType[] Convert_LethalMin_HazardToLibHazard(HazardType[] HazardTypes)
        {
            return HazardTypes.Select(HazardType => Convert_LethalMin_HazardToLibHazard(HazardType)).ToArray();
        }
    }
}