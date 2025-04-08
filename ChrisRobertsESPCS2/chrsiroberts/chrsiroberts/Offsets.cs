using System.Collections.Generic;

namespace chrsiroberts
{
    public static class Offsets
    {
        // Core offsets
        public const nint dwEntityList = 0x1A1F730;
        public const nint dwLocalPlayerController = 0x1A6E9C0;
        public const nint dwViewMatrix = 0x1A89130;
        // Entity offsets
        public const int m_iTeamNum = 0x3E3;              
        public const int m_iHealth = 0x344;               
        public const int m_vecAbsOrigin = 0xD0;          
        public const int m_pGameSceneNode = 0x328;        
        public const int m_hPlayerPawn = 0x814;          
        public const int m_modelState = 0x170;

        // Additional offsets (for future use) 
        public const int m_ArmorValue = 9244;           
        public const int m_bIsDefusing = 9194;          
        public const int m_iAccount = 64;               
        public const int m_pClippingWeapon = 0x13A0;     
        public const int m_pInGameMoneyServices = 1824;  
        public const int m_sSanitizedPlayerName = 1904;  

        // Bone indices
        public static Dictionary<string, int> Bones = new()
        {
            { "Waist", 0 },        // 0
            { "Neck", 5 },
            { "Head", 6 },
            { "ShoulderLeft", 8 },
            { "ForeLeft", 9 },
            { "HandLeft", 11 },
            { "ShoulderRight", 13 },
            { "ForeRight", 14 },
            { "KneeLeft", 23 },
            { "FeetLeft", 24 },
            { "KneeRight", 26 },
            { "FeetRight", 27 },

        };
    }
}
