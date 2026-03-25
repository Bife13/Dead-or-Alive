using System;
using UnityEngine;

[CreateAssetMenu(fileName = "DoAPalette", menuName = "Dead or Alive/Palette")]
public class DoAPalette : ScriptableObject
{
	private static DoAPalette _instance;

	public static DoAPalette Instance
	{
		get
		{
			if (_instance == null)
				_instance = Resources.Load<DoAPalette>("CurrentPalette");
			return _instance;
		}
	}

	[Header("Backgrounds")]
	public Color bg = Hex("0A0C12");

	public Color panel = Hex("12151E");
	public Color surface = Hex("1A1E2A");
	public Color border = Hex("2A3545");

	[Header("Text")]
	public Color textL1 = Hex("C8C0B0");

	public Color textL2 = Hex("8A90A0");
	public Color textL3 = Hex("6A7080");
	public Color textL4 = Hex("3A4050");

	[Header("Accents")]
	public Color ochre = Hex("C87C18");

	public Color ochreDim = Hex("C87C18", 0.4f); // modValue on IncomeDrain
	public Color verdigris = Hex("2A6858");
	public Color wine = Hex("5A0C12");
	public Color wineDim = Hex("5A0C12", 0.5f);
	public Color wineBright = Hex("9A2535"); // ZoneLockout, High threat
	public Color black = Hex("000000");

	[Header("Crew Identity")]
	public Color crewRookie = Hex("8A8060");

	public Color crewGhost = Hex("8A9AAA");
	public Color crewHandler = Hex("2A7A6A");
	public Color crewStrategist = Hex("6A7840");
	public Color crewEnforcer = Hex("6A3020");
	public Color crewConArtist = Hex("4A3868");
	public Color crewGunslinger = Hex("C89A30");

	public Color crewPawn = Hex("#5A5850");
	public Color crewDetonator = Hex("#784828");
	public Color crewLoner = Hex("#486068");
	public Color crewAnchor = Hex("#7A6040");
	public Color crewScavenger = Hex("#6A7035");


	public static Color Hex(string hex, float alpha = 1f)
	{
		ColorUtility.TryParseHtmlString("#" + hex, out Color c);
		c.a = alpha;
		return c;
	}

	public static Color ColorHueShift(Color color, float alpha = 1f)
	{
		color.a = alpha;
		return color;
	}

	public Color Resolve(PaletteToken.Token token)
	{
		return token switch
		{
			PaletteToken.Token.BG => bg,
			PaletteToken.Token.Panel => panel,
			PaletteToken.Token.Surface => surface,
			PaletteToken.Token.Border => border,
			PaletteToken.Token.TextL1 => textL1,
			PaletteToken.Token.TextL2 => textL2,
			PaletteToken.Token.TextL3 => textL3,
			PaletteToken.Token.TextL4 => textL4,
			PaletteToken.Token.Ochre => ochre,
			PaletteToken.Token.Verdigris => verdigris,
			PaletteToken.Token.Wine => wine,
			PaletteToken.Token.WineDim => wineDim,
			PaletteToken.Token.Black => black,
			_ => Color.magenta
		};
	}

	public Color GetCrewColor(CrewType type)
	{
		return type switch
		{
			CrewType.Rookie => crewRookie,
			CrewType.Ghost => crewGhost,
			CrewType.Handler => crewHandler,
			CrewType.Strategist => crewStrategist,
			CrewType.Enforcer => crewEnforcer,
			CrewType.ConArtist => crewConArtist,
			CrewType.Gunslinger => crewGunslinger,
			CrewType.Drone => crewConArtist,
			CrewType.Pawn => crewPawn,
			CrewType.Detonator => crewDetonator,
			CrewType.Anchor => crewAnchor,
			CrewType.Scavenger => crewScavenger,
			CrewType.Loner => crewLoner,
			_ => textL2
		};
	}
}