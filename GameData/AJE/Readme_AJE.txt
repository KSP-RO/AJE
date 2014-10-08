AJE is by camlost, with a bit of code here and there by NathanKell

For docs and readme see the forum thread.
http://forum.kerbalspaceprogram.com/threads/70008

AJE incorporates portions of NASA EngineSim; used according to NASA's license thereof with due credit.
AJE incorporates portions of JSBSim by  Jon S. Berndt, used in accordance with the LGPLv2

License is now LGPL v2, with the following exception, that the authors reserve the right to veto the inclusion of this in any "mod packs" at their sole discretion.

Changelog:
v1.5
	Backports from NathanKell's fork:
	*Redone piston engine support (inter/aftercoolers, multiple speed/stage superchargers, volumetric efficiency, ram air, and real performance for the included piston engines)
	*Jets show gross thrust as well as net thrust
	*Some intake area tweaks (still needs work to get sqft area of each intake part
	*Ported over all real jets and piston engines from RftS.
	*All included jets should perform like their real counterparts now. Real stats were used when available, educated guesses made when not.
	*Cleaning and variable-renaming in the EngineSim code for clarity and understanding
	*Update to 0.24.2 and FAR 0.14.1.1
	*Seamlessly works with or without RealFuels
	Fixes from taniwha to the DA (TVPP) engines and intakes.
	
v1.4
    Fixed some inlet data
    In-editor information of inlets and engines
    Added some engines and inlets
    Incorporated RftS textures for some B9 engines

v1.3
    Inlets
    A few more engines
    Added support for RAPIER, tweaked SABRE
    many DLL and CFG tweaks

v1.2
    Engines have 3% idle thrust by default
    Added TV_pizza support and several engines

v1.1.3
    The propellers should work correctly on other planets(untested) 
    Added a new engine F404

v1.1.2
    Solved compatibility issue with latest HotRocket

v1.1.1
    Balanced propellers, added a speed buff
    Support for KAX rotor wing

v1.1
    Added *realistic* support for propellers and rotary wings
    Supporting all FS and KAX propellers, including electric ones
    Stock "turbojet" becomes J58 with "bleed air" feature, thanks to NathenKell, who provided a simple and elegant solution. 
    
v1.0
    Combined everything into one module: AJEModule supports ME and MEFX at the same time
    Using stock atmosphere pressure and temperature now, engines should have ~10% more thrust at high altitude
    Engines have 10% more heat tolerance to compensate 
    AJE works without HotRockets now

v0.9 
    Fixed a bug with SABRE M
    New afterburner logic
    Upgraded J79 to J93, which is more powerful, especially for supersonic flight

v0.8
    Added CoM Offset to stock, B9 and FS engines, making RL rip-off designing more easy. 
    Air intakes has no physical effect now. Jet engines don't require intakeair, but they still need an atmosphere with oxygen to work. 
    Compatibility support