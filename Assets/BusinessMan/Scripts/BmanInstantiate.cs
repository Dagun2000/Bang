using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BmanInstantiate : MonoBehaviour
{

	// Use this for initialization
	private int faceT;
	private int skinT;
	private int eyeC;
	private int hairT;
	private int hairC;
	private int glassesT;
	private int jacketT;
	private int waistcoatT;
	private int handkerchiefT;
	private int tieT;
	private int beltT;
	private int jacketC;
	private int shirtC;
	private int waistcoatC;
	private int tieC;
	private int pantsC;
	private int shoesC;
	private int handkerchiefC;

	private BmanAssetsList assetsList;

	private SkinnedMeshRenderer skinnedMeshRenderer;

	public enum FaceType
	{
		FaceA,
		FaceB,
		FaceC,
		FaceD,
		FaceE

	}

	public enum SkinType
	{
		SkinA,
		SkinB,
		SkinC,
		SkinD,
		SkinE

	}

	public enum EyeColor
	{
		Brown,
		Blue,
		Green,
		Black,
		Gray,
		LightBrown

	}

	public enum Hair
	{
		HairA,
		HairB,
		HairC,
		HairD,
		HairE,
		HairF,
		HairG,
		HairH,
		HairI
	}

	public enum HairColor
	{
		Blond,
		Brown,
		Gray,
		Brunete,
		Black
	}

	public enum Glasses
	{
		No,
		Glasses,
		SunGlasses

	}


	public enum Tie
	{
		Tie,
		Butterfly,
		No

	}

	public enum Jacket
	{
		Open,
		Closed,
		No

	}

	public enum Waistcoat
	{
		Yes,
		No

	}

	public enum Belt
	{
		Yes,
		No

	}

	public enum Handkerchief
	{
		Yes,
		No

	}


	public enum JacketColor
	{
		Black,
		Charcoal,
		Navy,
		Grey,
		LightGray,
		White,
		Vintage,
		Blue,
		Tan,
		Brown
	}

	public enum ShirtColor
	{
		Black,
		Charcoal,
		Navy,
		Grey,
		LightGray,
		White,
		LightBlue,
		Blue
	}

	public enum WaistcoatColor
	{
		Black,
		Charcoal,
		Navy,
		Grey,
		LightGray,
		White,
		Vintage,
		Blue,
		Tan,
		Brown
	}

	public enum TieColor
	{
		Black,
		White,
		Blue,
		RedBlue,
		Red,
		BlueB,
		Purple,
		LightBlue,
		Gray,
		Brown
	}

	public enum PantsColor
	{
		Black,
		Charcoal,
		Navy,
		Grey,
		LightGray,
		White,
		Vintage,
		Blue,
		Tan,
		Brown
	}

	public enum ShoesColor
	{
		Black,
		Brown,
		RedBrown,
		White
	}

	public enum HandkerchiefColor
	{
		Black,
		White,
		Gray,
		Purple,
		Red,
		Blue
	}


	public Transform prefabObject;
	//
	public FaceType faceType;
	public SkinType skinType;
	public EyeColor eyeCol;
	public Glasses glasses;
	public Hair hair;
	public HairColor hairCol;
	public Jacket jacket;
	public Waistcoat waistcoat;
	public Tie tie;
	public Belt belt;
	public Handkerchief handkerchief;
	public JacketColor jacketCol;
	public ShirtColor shirtCol;
	public WaistcoatColor waistcoatCol;
	public TieColor tieCol;
	public PantsColor pantsCol;
	public ShoesColor shoesCol;
	public HandkerchiefColor handkerchiefCol;

	void Start ()
	{
		Transform pref = Instantiate (prefabObject, gameObject.transform.position, gameObject.transform.rotation);
		hairC = (int)hairCol;
		eyeC = (int)eyeCol;
		glassesT = (int)glasses;
		hairT = (int)hair;
		faceT = (int)faceType;
		//btmTyp = (int)bottomType;
		//topTyp = (int)topType;
		skinT = (int)skinType;
		jacketT = (int)jacket;
		waistcoatT = (int)waistcoat;
		tieT = (int)tie;
		handkerchiefT = (int)handkerchief;
		jacketC = (int)jacketCol;
		shirtC = (int)shirtCol;
		waistcoatC = (int)waistcoatCol;
		tieC = (int)tieCol;
		pantsC = (int)pantsCol;
		shoesC = (int)shoesCol;
		handkerchiefC = (int)handkerchiefCol;
		beltT = (int)belt;

		pref.gameObject.GetComponent<BmanCustomize> ().charCustomize (faceT, skinT, eyeC, glassesT, hairT, hairC, jacketT, waistcoatT, tieT, beltT, handkerchiefT, jacketC, shirtC, waistcoatC, tieC, pantsC, shoesC, handkerchiefC);
	}
	
	// Update is called once per frame
	void Update ()
	{
		
	}
}
