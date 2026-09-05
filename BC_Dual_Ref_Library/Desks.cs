using System.Collections.Generic;

namespace BC_Dual_Ref_Library
{
    public class Desks
    {
        public static Dictionary<string, long> deskLocationIDs = new Dictionary<string, long>()
        {
            // === CENTER PERKS (Centre_Perks) ===
            { "Centre_Perks|Desk_R_01 (1) (-25.5, 26.5, 0.0)", 81001 },         //Top Row Cell 1
            { "Centre_Perks|Desk_R_01 (2) (-19.5, 26.5, 0.0)", 81002 },         //Top Row Cell 2
            { "Centre_Perks|Desk_R_01 (3) (-13.5, 26.5, 0.0)", 81003 },         //Top Row Cell 3
            { "Centre_Perks|Desk_R_01 (4) (-7.5, 26.5, 0.0)", 81004 },          //Top Row Cell 4
            { "Centre_Perks|Desk_R_01 (5) (2.5, 26.5, 0.0)", 81005 },           //Top Row Cell 5
            { "Centre_Perks|Desk_R_01 (6) (8.5, 26.5, 0.0)", 81006 },           //Top Row Cell 6
            { "Centre_Perks|Desk_R_01 (7) (14.5, 26.5, 0.0)", 81007 },          //Top Row Cell 7
            { "Centre_Perks|Desk_R_01 (8) (20.5, 26.5, 0.0)", 81008 },          //Top Row Cell 8
            { "Centre_Perks|Desk_D_01 (1) (-4.5, 19.5, 0.0)", 81009 },          //Middle Row Cell 1
            { "Centre_Perks|Desk_D_01 (2) (3.5, 19.5, 0.0)", 81010 },           //Middle Row Cell 2
            { "Centre_Perks|Desk_D_01 (3) (-4.5, 13.5, 0.0)", 81011 },          //Middle Row Cell 3
            { "Centre_Perks|Desk_D_01 (4) (3.5, 13.5, 0.0)", 81012 },           //Middle Row Cell 4
            { "Centre_Perks|Desk_D_01 (5) (-4.5, 3.5, 0.0)", 81013 },           //Middle Row Cell 5
            { "Centre_Perks|Desk_D_01 (6) (3.5, 3.5, 0.0)", 81014 },            //Middle Row Cell 6
            { "Centre_Perks|Desk_D_01 (7) (3.5, -2.5, 0.0)", 81015 },           //Middle Row Cell 8
            { "Centre_Perks|Desk_D_01 (8) (-4.5, -2.5, 0.0)", 81016 },          //Middle Row Cell 7
            { "Centre_Perks|Desk_L_01 (-21.5, -13.5, 0.0)", 81017 },            //Bottom Row Cell 1
            { "Centre_Perks|Desk_L_01 (1) (-15.5, -13.5, 0.0)", 81018 },        //Bottom Row Cell 2
            { "Centre_Perks|Desk_L_01 (2) (-9.5, -13.5, 0.0)", 81019 },         //Bottom Row Cell 3
            { "Centre_Perks|Desk_L_01 (3) (-3.5, -13.5, 0.0)", 81020 },         //Bottom Row Cell 4
            { "Centre_Perks|Desk_L_01 (4) (6.5, -13.5, 0.0)", 81021 },          //Bottom Row Cell 5
            { "Centre_Perks|Desk_L_01 (5) (12.5, -13.5, 0.0)", 81022 },         //Bottom Row Cell 6
            { "Centre_Perks|Desk_L_01 (6) (18.5, -13.5, 0.0)", 81023 },         //Bottom Row Cell 7
            { "Centre_Perks|Desk_L_01 (7) (24.5, -13.5, 0.0)", 81024 },         //Bottom Row Cell 8
            { "Centre_Perks|Cutlery_Tray_Down (-32.5, 10.5, 0.0)", 81025 },     //Cutlery Tray
            { "Centre_Perks|Desk_D_01 (1) (-8.5, -38.5, 0.0)", 81026 },         //Guard Cell 3 (Red Door)
            { "Centre_Perks|Desk_D_01 (4) (-17.5, 37.5, 0.0)", 81027 },         //Desk next to car park
            { "Centre_Perks|Desk_D_01 (-16.5, -38.5, 0.0)", 81028 },            //Guard Cell 1 (Red Door)
            { "Centre_Perks|Desk_D_01 (33.5, 3.5, 0.0)", 81029 },               //Contraband Desk (Red Door & Guard outfit)
            { "Centre_Perks|Desk_D_01 (9) (-41.5, -1.5, 0.0)", 81030 },         //Maintenance Desk Floor 0 next to infirmary
            { "Centre_Perks|Desk_D_01 (2) (-7.5, 28.5, -6.0)", 81031 },         //Room above gym Floor 1
            { "Centre_Perks|Desk_D_01 (1) (41.5, 26.5, -6.0)", 81032 },         // Maintenance Desk Floor 1 Right Hand Side
            { "Centre_Perks|Desk_D_01 (1) (6.5, -11.5, -6.0)", 81033 },         //Classroom Desk Floor 1
            { "Centre_Perks|Desk_D_01 (3) (-19.5, 27.5, -6.0)", 81034 },        //Meet the Crew Desk
            { "Centre_Perks|Desk_L_01 (-38.5, 1.5, -6.0)", 81035 },             //Medic Desk Floor 1 (Need nurse outfit?)
            { "Centre_Perks|Desk_D_01 (-0.5, 30.5, -12.0)", 81036 },            //Center North on Roof (Red Door)
            //ALSO A MULTIPLAYER DESK NOT MAPPED
            
            // === RATTLESNAKE SPRINGS (OldWestFort) ===
            { "OldWestFort|Desk_D_01 (31.5, 9.5, 0.0)", 82001 },                //
            { "OldWestFort|Desk_U_01 (-28.5, 5.5, 0.0)", 82002 },               //
            { "OldWestFort|Desk_D_01 (8) (21.5, 14.5, -6.0)", 82003 },          //
            { "OldWestFort|Desk_U_01 (-29.5, -12.5, 0.0)", 82004 },             //
            { "OldWestFort|Desk_R_01 (1) (-15.5, 38.5, 0.0)", 82005 },          //
            { "OldWestFort|Desk_D_01 (11.5, 14.5, -6.0)", 82006 },              //
            { "OldWestFort|Desk_U_01 (-29.5, -18.5, 0.0)", 82007 },             //
            { "OldWestFort|Cutlery_Tray_Down (29.5, -23.5, 0.0)", 82008 },      //
            { "OldWestFort|Desk_D_01 (6) (-4.5, -28.5, -6.0)", 82009 },         //
            { "OldWestFort|Desk_U_01 (24.5, 5.5, 0.0)", 82010 },                //
            { "OldWestFort|Desk_R_01 (-15.5, 37.5, 0.0)", 82011 },              //
            { "OldWestFort|Desk_U_01 (-29.5, -24.5, 0.0)", 82012 },             //
            { "OldWestFort|Desk_D_01 (10) (-10.5, 43.5, -6.0)", 82013 },        //
            { "OldWestFort|Desk_D_01 (26.5, 45.5, -6.0)", 82014 },              //
            { "OldWestFort|Desk_U_01 (1) (-33.5, 25.5, 0.0)", 82015 },          //
            { "OldWestFort|Desk_R_01 (-21.5, -31.5, -6.0)", 82016 },            //
            { "OldWestFort|Desk_R_01 (-33.5, 14.5, -6.0)", 82017 },             //
            { "OldWestFort|Desk_D_01 (-28.5, -2.5, 0.0)", 82018 },              //
            { "OldWestFort|Desk_D_01 (13) (16.5, 12.5, -12.0)", 82019 },        //
            { "OldWestFort|Desk_D_01 (11) (3.5, 48.5, -6.0)", 82020 },          //
            { "OldWestFort|Desk_D_01 (-29.5, 9.5, 0.0)", 82021 },               //
            { "OldWestFort|Desk_D_01 (37.5, 26.5, 0.0)", 82022 },               //
            { "OldWestFort|Desk_D_01 (-30.5, 3.5, 0.0)", 82023 },               //
            { "OldWestFort|Desk_D_01 (-30.5, -20.5, 0.0)", 82024 },             //
            { "OldWestFort|Desk_D_01 (-18.5, 43.5, -6.0)", 82025 },             //
            { "OldWestFort|Desk_D_01 (5) (14.5, -30.5, -6.0)", 82026 },         //
            { "OldWestFort|Desk_D_01 (2) (11.5, 1.5, -6.0)", 82027 },           //
            { "OldWestFort|Desk_D_01 (3) (-25.5, 25.5, -6.0)", 82028 },         //
            { "OldWestFort|Desk_D_01 (-30.5, -8.5, 0.0)", 82029 },              //
            { "OldWestFort|Desk_U_01 (-29.5, -0.5, 0.0)", 82030 },              //
            { "OldWestFort|Desk_D_01 (12) (2.5, 49.5, -12.0)", 82031 },         //
            { "OldWestFort|Desk_D_01 (9) (0.5, -0.5, -6.0)", 82032 },           //
            { "OldWestFort|Desk_D_01 (5.5, 27.5, 3.0)", 82033 },                //
            { "OldWestFort|Desk_D_01 (14) (28.5, 6.5, -12.0)", 82034 },         //
            { "OldWestFort|Desk_D_01 (-29.5, -14.5, 0.0)", 82035 },             //
            { "OldWestFort|Desk_D_01 (1) (34.5, 20.5, -6.0)", 82036 },          //
            { "OldWestFort|Desk_D_01 (4) (-4.5, -42.5, -6.0)", 82037 },         //
            { "OldWestFort|Desk_U_01 (-29.5, -6.5, 0.0)", 82038 },              //
 
            // === K.A.P.O.W. CAMP (POW_Camp) ===
            { "POW_Camp|Desk_U_01 (1) (-14.5, -21.5, 0.0)", 83001 },            //Cell 4 Desk 4
            { "POW_Camp|Desk_D_01 (-18.5, -15.5, 0.0)", 83002 },                // Cell 4 Desk 1
            { "POW_Camp|Desk_D_01 (4) (-18.5, 8.5, 0.0)", 83003 },              // Cell 2 Desk 1
            { "POW_Camp|Desk_D_01 (20.5, 26.5, -6.0)", 83004 },                 // Left Guard Room Desk Floor 2 (Red Door) 
            { "POW_Camp|Desk_U_01 (7) (-18.5, 14.5, 0.0)", 83005 },             //Cell 1 Desk 3
            { "POW_Camp|Desk_D_01 (1) (21.5, 25.5, 0.0)", 83006 },              //Contraband Desk (Red Door AND Guard Outfit)
            { "POW_Camp|Desk_D_01 (2) (-14.5, -3.5, 0.0)", 83007 },             //Cell 3 Desk 2
            { "POW_Camp|Desk_U_01 (-16.5, 30.5, 0.0)", 83008 },                 //Maintenance Desk Floor 1(Cyan Door)
            { "POW_Camp|Desk_R_01 (9.5, 12.5, -6.0)", 83009 },                  //Green Lounge Desk Floor 2
            { "POW_Camp|Desk_D_01 (-8.5, 35.5, 0.0)", 83010 },                  //Medic Desk Floor 1 (Infirmary Outfit)
            { "POW_Camp|Desk_D_01 (39.5, 0.5, -6.0)", 83011 },                  //Maintenance Desk Floor 2 (Cyan Door)
            { "POW_Camp|Desk_U_01 (2) (-14.5, -9.5, 0.0)", 83012 },             //Cell 3 Desk 4
            { "POW_Camp|Desk_D_01 (6) (-14.5, 20.5, 0.0)", 83013 },             //Cell 1 Desk 2
            { "POW_Camp|Desk_D_01 (2) (-32.5, -30.4, 0.0)", 83014 },            //Motorcycle Room Desk (Vent Tools)
            { "POW_Camp|Desk_D_01 (5) (-14.5, 8.5, 0.0)", 83015 },              //Cell 2 Desk 2
            { "POW_Camp|Desk_U_01 (4) (-18.5, 2.5, 0.0)", 83016 },              //Cell 2 Desk 3
            { "POW_Camp|Desk_D_01 (3) (11.5, -22.5, 0.0)", 83017 },             //Southern Storage Desk Floor 1
            { "POW_Camp|Desk_U_01 (6) (-14.5, 14.5, 0.0)", 83018 },             //Cell 1 Desk 4
            { "POW_Camp|Desk_D_01 (1) (-14.5, -15.5, 0.0)", 83019 },            //Cell 4 Desk 2
            { "POW_Camp|Desk_U_01 (-18.5, -21.5, 0.0)", 83020 },                //Cell 4 Desk 2? (Check which one is supposed to be 3)
            { "POW_Camp|Desk_D_01 (1) (-30.5, 34.5, 0.0)", 83021 },             //Multiplayer
            { "POW_Camp|Cutlery_Tray_Down (16.5, 6.5, 0.0)", 83022 },           //Cutlery Tray
            { "POW_Camp|Desk_D_01 (3) (-18.5, -3.5, 0.0)", 83023 },             //Cell 3 Desk 1
            { "POW_Camp|Desk_U_01 (3) (-18.5, -9.5, 0.0)", 83024 },             //Cell 3 Desk 3
            { "POW_Camp|Desk_D_01 (7) (-18.5, 20.5, 0.0)", 83025 },             //Cell 1 Desk 1
            { "POW_Camp|Desk_D_01 (1) (21.5, 26.5, -6.0)", 83026 },             //Right Guard Room Desk Floor 2 (Red Door)
            { "POW_Camp|Desk_U_01 (5) (-14.5, 2.5, 0.0)", 83027 },              //Cell 2 Desk 4
            
            // === HMP OFFSHORE (Oil_Rig) ===
            { "Oil_Rig|Desk_U_01 (3) (-18.5, -11.5, -18.0)", 84001 },           //
            { "Oil_Rig|Desk_U_01 (13) (-40.5, 19.5, -24.0)", 84002 },           //
            { "Oil_Rig|Desk_R_01 (0.5, 11.5, -24.0)", 84003 },                  //Maintenance Desk South East Map Floor 7 (Cyan Door)
            { "Oil_Rig|Desk_U_01 (-44.5, 11.5, -3.0)", 84004 },                 //Bottom of Left Stairwell (Red Door)
            { "Oil_Rig|Desk_U_01 (5) (-18.5, 0.5, -18.0)", 84005 },             //
            { "Oil_Rig|Desk_U_01 (10) (-18.5, -4.5, -24.0)", 84006 },           //
            { "Oil_Rig|Desk_U_01 (11) (-18.5, -10.5, -24.0)", 84007 },          //
            { "Oil_Rig|Desk_D_01 (-11.5, 41.5, -36.0)", 84008 },                //Maintenance Desk North Map (Next to Cyan Key Card room) Floor 9 (Cyan Door)
            { "Oil_Rig|Desk_R_01 (1) (-43.5, 20.5, -12.0)", 84009 },            //Left Stairwell (Red Door)
            { "Oil_Rig|Desk_D_01 (-40.5, 35.5, -24.0)", 84010 },                //Multiplayer
            { "Oil_Rig|Desk_R_01 (17.5, -34.5, -12.0)", 84011 },                //Maintenance Desk In Security Area (Cyan AND Red Door)
            { "Oil_Rig|Desk_R_01 (2.5, 27.5, -18.0)", 84012 },                  //Multiplayer
            { "Oil_Rig|Desk_D_01 (6.5, 13.5, -6.0)", 84013 },                   //Right Stairwell (Red Door)
            { "Oil_Rig|Desk_U_01 (1) (21.5, -26.5, -18.0)", 84014 },            //Guard Area (Red Door)
            { "Oil_Rig|Desk_R_01 (-14.5, 29.5, -30.0)", 84015 },                //Medic Desk next to job office (Nurses outfit)
            { "Oil_Rig|Desk_U_01 (2) (-23.5, -11.5, -18.0)", 84016 },           //
            { "Oil_Rig|Cutlery_Tray_Down (-19.6, 29.5, -24.0)", 84017 },        //
            { "Oil_Rig|Desk_U_01 (12) (-37.5, 4.5, -24.0)", 84018 },            //
            { "Oil_Rig|Desk_R_01 (-41.5, 41.5, -18.0)", 84019 },                //Maintenance Desk Floor 6 (Cyan Door)
            { "Oil_Rig|Desk_U_01 (7) (-23.5, -4.5, -24.0)", 84020 },            //
            { "Oil_Rig|Desk_U_01 (6) (-31.5, 32.5, -18.0)", 84021 },            //
            { "Oil_Rig|Desk_U_01 (21.5, -20.5, -18.0)", 84022 },                //Guard Area (Red Door)
            { "Oil_Rig|Desk_U_01 (4) (-18.5, -5.5, -18.0)", 84023 },            //
            { "Oil_Rig|Desk_U_01 (8) (-23.5, -10.5, -24.0)", 84024 },           //
            { "Oil_Rig|Desk_D_01 (1.5, 30.5, -18.0)", 84025 },                  //Multiplayer
            { "Oil_Rig|Desk_D_01 (30.5, -32.5, -12.0)", 84026 },                //Contraband Desk (Red Door AND Guard Outfit)
            { "Oil_Rig|Desk_U_01 (-41.5, 28.5, -24.0)", 84027 },                //Multiplayer
            { "Oil_Rig|Desk_R_01 (1) (-34.5, 37.5, -36.0)", 84028 },            //Multiplayer
            { "Oil_Rig|Desk_U_01 (9) (-18.5, 1.5, -24.0)", 84029 },             //
            { "Oil_Rig|Desk_U_01 (6) (-23.5, 1.5, -24.0)", 84030 },             //
            { "Oil_Rig|Desk_R_01 (-27.5, 38.5, -36.0)", 84031 },                //
            { "Oil_Rig|Desk_U_01 (1) (-23.5, -5.5, -18.0)", 84032 },            //
            { "Oil_Rig|Desk_U_01 (14) (7.5, 17.5, -24.0)", 84033 },             //
            { "Oil_Rig|Desk_U_01 (-23.5, 0.5, -18.0)", 84034 },                 //
 
            // === FORT TUNDRA (Gulag_Prison) ===
            { "Gulag_Prison|Desk_L_01 (3) (14.5, 0.5, 0.0)", 85001 },           //
            { "Gulag_Prison|Desk_L_01 (5) (14.5, 6.5, 0.0)", 85002 },           //
            { "Gulag_Prison|Desk_L_01 (5) (14.5, 7.5, -6.0)", 85003 },          //
            { "Gulag_Prison|Desk_R_01 (5) (-21.5, 0.5, 0.0)", 85004 },          //
            { "Gulag_Prison|Desk_R_01 (-21.5, -15.5, 0.0)", 85005 },            //
            { "Gulag_Prison|Cutlery_Tray_Down (-21.5, -32.5, 0.0)", 85006 },    //
            { "Gulag_Prison|Desk_L_01 (4) (14.5, 3.5, 0.0)", 85007 },           //
            { "Gulag_Prison|Desk_D_01 (5.5, -23.5, -6.0)", 85008 },             //
            { "Gulag_Prison|Desk_R_01 (1) (-21.5, -12.5, 0.0)", 85009 },        //
            { "Gulag_Prison|Desk_L_01 (4) (14.5, 4.5, -6.0)", 85010 },          //
            { "Gulag_Prison|Desk_D_01 (1) (21.5, 25.5, 0.0)", 85011 },          //
            { "Gulag_Prison|Desk_R_01 (3) (-21.5, 7.5, -6.0)", 85012 },         //
            { "Gulag_Prison|Desk_L_01 (1) (14.5, -12.5, 0.0)", 85013 },         //
            { "Gulag_Prison|Desk_R_01 (4) (-21.5, 3.5, 0.0)", 85014 },          //
            { "Gulag_Prison|Desk_D_01 (-32.5, -2.5, 0.0)", 85015 },             //
            { "Gulag_Prison|Desk_D_01 (34.5, 0.5, 0.0)", 85016 },               //
            { "Gulag_Prison|Desk_R_01 (32.5, 28.5, 0.0)", 85017 },              //
            { "Gulag_Prison|Desk_D_01 (12.5, -35.5, 0.0)", 85018 },             //
            { "Gulag_Prison|Desk_R_01 (4) (-21.5, 4.5, -6.0)", 85019 },         //
            { "Gulag_Prison|Desk_R_01 (2) (-21.5, -9.5, 0.0)", 85020 },         //
            { "Gulag_Prison|Desk_R_01 (1) (-21.5, -11.5, -6.0)", 85021 },       //
            { "Gulag_Prison|Desk_D_01 (-27.5, 13.5, 0.0)", 85022 },             //
            { "Gulag_Prison|Desk_R_01 (2) (-21.5, -8.5, -6.0)", 85023 },        //
            { "Gulag_Prison|Desk_D_01 (1) (29.5, 43.5, 0.0)", 85024 },          //
            { "Gulag_Prison|Desk_L_01 (2) (14.5, -9.5, 0.0)", 85025 },          //
            { "Gulag_Prison|Desk_L_01 (2) (14.5, -8.5, -6.0)", 85026 },         //
            { "Gulag_Prison|Desk_D_01 (1) (20.5, -35.5, 0.0)", 85027 },         //
            { "Gulag_Prison|Desk_D_01 (1) (11.5, -23.5, -6.0)", 85028 },        //
            { "Gulag_Prison|Desk_L_01 (14.5, -14.5, -6.0)", 85029 },            //
            { "Gulag_Prison|Desk_R_01 (-21.5, -14.5, -6.0)", 85030 },           //
            { "Gulag_Prison|Desk_R_01 (5) (-21.5, 1.5, -6.0)", 85031 },         //
            { "Gulag_Prison|Desk_R_01 (3) (-21.5, 6.5, 0.0)", 85032 },          //
            { "Gulag_Prison|Desk_D_01 (2) (25.5, 13.5, -6.0)", 85033 },         //
            { "Gulag_Prison|Desk_L_01 (3) (14.5, 1.5, -6.0)", 85034 },          //
            { "Gulag_Prison|Desk_L_01 (14.5, -15.5, 0.0)", 85035 },             //
            { "Gulag_Prison|Desk_L_01 (1) (14.5, -11.5, -6.0)", 85036 },        //
 
            // === AREA 17 (Area_17) ===
            { "Area_17|Desk_U_01 (4) (-14.5, -17.5, 3.0)", 86001 },             //Desk Outside Infirmary (Underground)
            { "Area_17|Desk_R_01 (30.5, 26.5, -6.0)", 86002 },                  //Cell 4 Floor 2
            { "Area_17|Desk_U_01 (2) (27.5, 34.5, -6.0)", 86003 },              //Cell 3 Floor 2
            { "Area_17|Desk_U_01 (3) (-36.5, 3.5, 3.0)", 86004 },               //Western Stray Underground
            { "Area_17|Desk_L_01 (34.5, 10.5, 3.0)", 86005 },                   //Maintenance Desk Underground (Check for red key vs Cyan)
            { "Area_17|Desk_D_01 (3) (31.5, 0.5, -6.0)", 86006 },               //Guard Lounge Floor 2 (Red Door)
            { "Area_17|Desk_D_01 (1) (2.5, -11.5, 3.0)", 86007 },               //Medic Desk Underground (Infirmary Outfit)
            { "Area_17|Desk_D_01 (2) (-2.5, 9.5, 0.0)", 86008 },                //Musician Guard Desk Floor 1 (Red Door)
            { "Area_17|Desk_R_01 (30.5, 25.5, 0.0)", 86009 },                   //Cell 4 Floor 1
            { "Area_17|Desk_U_01_Lunar (-33.5, 15.6, 0.0)", 86010 },            //Multiplayer
            { "Area_17|Desk_L_01 (11.5, -13.5, 0.0)", 86011 },                  //Outdoor Maintenance Desk Floor 1 (Red Key)
            { "Area_17|Desk_U_01 (5) (38.5, 43.5, -6.0)", 86012 },              //Cell Outskirts North east Floor 2 (Red Door)
            { "Area_17|Cutlery_Tray_Down (-12.0, 26.9, 3.0)", 86013 },          //Cutlery Tray
            { "Area_17|Desk_L_01 (10.5, 26.5, -6.0)", 86014 },                  //Floor 2 Cell 1
            { "Area_17|Desk_D_01 (29.5, 4.5, 0.0)", 86015 },                    //Guard Lounge Floor 1 (Red Door)
            { "Area_17|Desk_U_01 (1) (13.5, 34.5, -6.0)", 86016 },              //Cell 2 Floor 2
            { "Area_17|Desk_U_01 (1) (27.5, 33.5, 0.0)", 86017 },               //Cell 3 Floor 1
            { "Area_17|Desk_D_01 (1) (33.5, 14.5, 0.0)", 86018 },               //Northern Guard Storage Floor 1 (Red Door)
            { "Area_17|Desk_U_01 (6) (39.5, 21.5, 0.0)", 86019 },               //Cell Outskirts South East Desk Floor 1 (Red Door)
            { "Area_17|Desk_U_01 (3) (29.5, -26.5, 3.0)", 86020 },              //Multiplayer
            { "Area_17|Desk_U_01 (13.5, 33.5, 0.0)", 86021 },                   //Floor 1 Cell 2
            { "Area_17|Desk_D_01 (33.5, -4.5, 0.0)", 86022 },                   //Floor 1 Southern Guard Storage Desk
            { "Area_17|Desk_U_01 (1) (38.5, -3.5, 3.0)", 86023 },               //Eastern Stray Underground
            { "Area_17|Desk_D_01 (4) (4.5, 10.5, -6.0)", 86024 },               //Guard Storage Floor 2 (Check for Red Door)
            { "Area_17|Desk_U_01 (2) (-21.5, -11.5, 3.0)", 86025 },             //Multiplayer
            { "Area_17|Desk_R_01 (-4.5, 10.5, 3.0)", 86026 },                   //Contraband Desk Underground (Red door and Guard outfit)
            { "Area_17|Desk_U_01 (5) (22.5, 42.5, 0.0)", 86027 },               //Cell Outskirts: Northern Desk Floor 1 (Red Door)
            { "Area_17|Desk_U_01 (6) (2.5, 21.5, -6.0)", 86028 },               //Cell Outskirts South West Desk Floor 2 (Red Key)
            { "Area_17|Desk_U_01 (7) (1.5, -25.5, 0.0)", 86029 },               //UFO Room Desk Floor 1
            { "Area_17|Desk_D_01 (1) (13.5, 13.5, -6.0)", 86030 },              //Guard Office Desk Floor 2
            { "Area_17|Desk_U_01 (1) (44.5, 31.5, 3.0)", 86031 },               //Alien Pod Room Guard Desk Underground (Red Door)
            { "Area_17|Desk_L_01 (10.5, 25.5, 0.0)", 86032 },                   //Cell 1 Floor 1
            { "Area_17|Desk_U_01 (2) (26.5, -27.5, 3.0)", 86033 },              //Multiplayer
 
          
            // === U.S.S. ANOMALY (Space Prison) ===
            { "Space Prison|Desk_D_01 (-12.5, 8.5, 0.0)", 87001 },              //
            { "Space Prison|Desk_U_01 (8) (6.5, -33.5, 0.0)", 87002 },          //
            { "Space Prison|Desk_U_01 (11) (-20.5, -33.5, 0.0)", 87003 },       //
            { "Space Prison|Desk_U_01 (1) (-14.5, -23.5, 0.0)", 87004 },        //
            { "Space Prison|Desk_D_01 (1) (-9.5, 29.5, 0.0)", 87005 },          //
            { "Space Prison|Desk_D_01 (40.5, -12.5, 0.0)", 87006 },             //
            { "Space Prison|Desk_U_01 (5) (18.5, -23.5, 0.0)", 87007 },         //
            { "Space Prison|Desk_U_01 (-20.5, -23.5, 0.0)", 87008 },            //
            { "Space Prison|Desk_D_01 (1) (45.5, 6.5, -6.0)", 87009 },          //
            { "Space Prison|Desk_D_01 (34.5, -12.5, 0.0)", 87010 },             //
            { "Space Prison|Desk_D_01 (4) (13.5, 25.5, -6.0)", 87011 },         //
            { "Space Prison|Desk_U_01 (3) (6.5, -23.5, 0.0)", 87012 },          //
            { "Space Prison|Desk_D_01 (1) (49.5, -27.5, -6.0)", 87013 },        //
            { "Space Prison|Desk_D_01 (-11.5, 43.5, -6.0)", 87014 },            //
            { "Space Prison|Desk_U_01 (2) (-8.5, -23.5, 0.0)", 87015 },         //
            { "Space Prison|Desk_U_01 (9) (-8.5, -33.5, 0.0)", 87016 },         //
            { "Space Prison|Desk_D_01 (36.5, -16.5, -0.3)", 87017 },            //
            { "Space Prison|Desk_U_01 (-6.5, 8.5, 0.0)", 87018 },               //
            { "Space Prison|Desk_U_01 (4) (12.5, -23.5, 0.0)", 87019 },         //
            { "Space Prison|Desk_U_01 (10) (-14.5, -33.5, 0.0)", 87020 },       //
            { "Space Prison|Desk_D_01 (2) (-35.5, -16.5, 0.0)", 87021 },        //
            { "Space Prison|Desk_U_01 (7) (12.5, -33.5, 0.0)", 87022 },         //
            { "Space Prison|Cutlery_Tray_Down (18.5, 5.3, 0.0)", 87023 },       //
            { "Space Prison|Desk_D_01 (6.5, 8.5, 0.0)", 87024 },                //
            { "Space Prison|Desk_D_01 (3) (-18.5, 11.5, -6.0)", 87025 },        //
            { "Space Prison|Desk_D_01 (2) (-35.5, 13.5, -6.0)", 87026 },        //
            { "Space Prison|Desk_U_01 (6) (18.5, -33.5, 0.0)", 87027 },         //
            { "Space Prison|Desk_D_01 (3) (45.5, 12.5, 0.0)", 87028 },          //
            
            
            // === COUGAR CREEK RAILROAD (Transport_Train) ===
            { "Transport_Train|Desk_R_01 (30.5, -1.5, 3.0)", 88001 },           //Kitchen Desk
            { "Transport_Train|Desk_D_01 (2) (-28.5, 0.5, 3.0)", 88002 },       //Contraband Desk in guards office
            { "Transport_Train|Desk_D_01 (1) (-88.5, 0.5, 3.0)", 88003 },       //Second to last carriage (With the horse)
            { "Transport_Train|Desk_U_01 (55.5, -3.5, 3.0)", 88004 },           //Carriage for Grapple Escape
            { "Transport_Train|Desk_U_01 (-11.5, -1.5, 3.0)", 88005 },          //Medic Desk
            { "Transport_Train|Desk_R_01 (80.5, -1.5, 3.0)", 88006 },           //First class  Desk
            { "Transport_Train|Desk_D_01 (-103.5, 0.5, 3.0)", 88007 },          //Last Carriage 
            { "Transport_Train|Desk_D_01 (3) (7.5, 0.5, 3.0)", 88008 },         //Carriage to right of medics
            //THERE IS ONE BEHIND THE MULTIPLAYER DOOR BUT IT'S NOT MAPPED
            
            // === H.M.S. ORCA (Transport_Boat) ===
            { "Transport_Boat|Desk_D_01 (12.5, 4.5, -3.0)", 89001 },            // Guard Rest Room Floor 3
            { "Transport_Boat|Desk_U_01 (-9.5, -3.5, -3.0)", 89002 },           //Room to right of control panel Desk 1 (Left) Floor 3
            { "Transport_Boat|Desk_R_01 (-20.5, 1.5, 0.0)", 89003 },            // CANT ACCESS IN SINGLE PLAYER (FLOOR 2)
            { "Transport_Boat|Desk_R_01 (-12.5, -8.5, 0.0)", 89004 },           // By Cells Next to Stairs Floor 2
            { "Transport_Boat|Desk_D_01 (17.5, -0.5, 3.0)", 89005 },            //Engine Room Left Desk Floor 1
            { "Transport_Boat|Desk_D_01 (9.5, -8.5, 3.0)", 89006 },             //Office to left of engine room Desk Floor 1
            { "Transport_Boat|Desk_U_01 (12.5, -4.5, 0.0)", 89007 },            // Medic Desk Floor 2
            { "Transport_Boat|Desk_R_01 (-12.5, -7.5, -3.0)", 89008 },          //Room 'below' control panel Floor 3
            { "Transport_Boat|Desk_D_01 (3.5, -7.5, -3.0)", 89009 },            //Room to right of control panel Desk 1 (Right) Floor 3
            { "Transport_Boat|Desk_R_01 (5.5, -4.5, -3.0)", 89010 },            //Room to left of the 2 staircases Floor 3
            { "Transport_Boat|Desk_D_01 (-23.5, -7.5, 3.0)", 89011 },           // Library Desk Floor 1
            { "Transport_Boat|Desk_U_01 (-14.5, -3.5, 3.0)", 89012 },           //Solitary Office Desk Floor 1
            { "Transport_Boat|Desk_D_01 (9.5, 2.5, 3.0)", 89013 },              //Desk in top of corridor Floor 1
            { "Transport_Boat|Desk_U_01 (-5.5, -11.5, 0.0)", 89014 },           // Contrband Desk Floor 2
            { "Transport_Boat|Desk_D_01 (1) (24.5, -9.5, 3.0)", 89015 },        //Engine Room Right Desk Floor 1
            
            // === AIR FORCE CON (Transport_Plane)
            { "Transport_Plane|Desk_R_01 (0.5, 16.5, -3.0)", 90001 },           //Guard Rest Room Floor 3
            { "Transport_Plane|Desk_L_01 (2) (4.5, 9.5, 0.0)", 90002 },         // Mid Plane floor 2
            { "Transport_Plane|Desk_R_01 (1) (-4.5, 38.5, 0.0)", 90003 },       //Next to control panel top of plane Floor 2
            { "Transport_Plane|Desk_D_01 (-3.5, 11.5, 3.0)", 90004 },           //Room below solitary floor 1
            { "Transport_Plane|Desk_L_01 (1) (4.5, -6.5, -3.0)", 90005 },       //Hallway above kitchen Floor 3
            { "Transport_Plane|Desk_L_01 (1) (1.5, -10.5, 0.0)", 90006 },       //Contraband Desk in guards room Floor 2
            { "Transport_Plane|Desk_L_01 (1) (4.5, -4.5, 3.0)", 90007 },        //CANT ACCESS IN SINGLE PLAYER (FLOOR 1)
            { "Transport_Plane|Desk_U_01 (1) (1.5, -19.5, -3.0)", 90008 },      //Below Kitchen Floor 3
            { "Transport_Plane|Desk_U_01 (4.5, 3.5, -3.0)", 90009 },            //Medic Desk Floor 3
            { "Transport_Plane|Desk_R_01 (-5.5, 28.5, -3.0)", 90010 },          //Office Floor 3
            { "Transport_Plane|Desk_R_01 (-5.5, 1.5, 3.0)", 90011 },            //Left room on double doors Floor 1
            { "Transport_Plane|Desk_D_01 (-5.5, -1.5, -3.0)", 90012 },          //Contraband Desk Floor 3

        };
    }
}