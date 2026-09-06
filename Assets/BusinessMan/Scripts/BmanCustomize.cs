using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BmanCustomize : MonoBehaviour
{

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
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		
	}

	public void charCustomize (int faceTy, int skinTy, int eyeCo, int glassesTy, int hairTy, int hairCo, int jacketTy, int waistcoatTy, int tieTy, int beltTy, int handkerchiefTy, int jacketCo, int shirtCo, int waistcoatCo, int tieCo, int pantsCo, int shoesCo, int handkerchiefCo)
	{
		Material[] mat;
		assetsList = gameObject.GetComponent<BmanAssetsList> ();
		foreach (Transform child in assetsList.EyeObj.transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.Eye_Materials [eyeCo];
		}



		foreach (Transform child in assetsList.ClosedJacketObj.transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.JacketMaterials [jacketCo];
		}

		foreach (Transform child in assetsList.OpenJacketObj.transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.JacketMaterials [jacketCo];
		}

		foreach (Transform child in assetsList.ClosedShirtObj.transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.ShirtMaterials [shirtCo];
		}
		foreach (Transform child in assetsList.OpenShirtObj.transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.ShirtMaterials [shirtCo];
		}
		foreach (Transform child in assetsList.ShirtObj.transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.ShirtMaterials [shirtCo];
		}
		foreach (Transform child in assetsList.ShirtWObj.transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.ShirtMaterials [shirtCo];
		}

		foreach (Transform child in assetsList.ClosedWaistcoatObj.transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.WaistcoatMaterials [waistcoatCo];
		}
		foreach (Transform child in assetsList.OpenWaistcoatObj.transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.WaistcoatMaterials [waistcoatCo];
		}
		foreach (Transform child in assetsList.WaistcoatObj.transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.WaistcoatMaterials [waistcoatCo];
		}

		foreach (Transform child in assetsList.ButterflyTieObj.transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.TieMaterials [tieCo];
		}
		foreach (Transform child in assetsList.TieObj.transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.TieMaterials [tieCo];
		}
		foreach (Transform child in assetsList.ClosedTieObj.transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.TieMaterials [tieCo];
		}
		foreach (Transform child in assetsList.OpenTieWObj.transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.TieMaterials [tieCo];
		}

		foreach (Transform child in assetsList.PantsObj.transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.PantsMaterials [pantsCo];
		}

		foreach (Transform child in assetsList.PantsLoopsObj.transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.PantsMaterials [pantsCo];
		}

		foreach (Transform child in assetsList.ShoesObj.transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.ShoesMaterials [shoesCo];
		}

		foreach (Transform child in assetsList.OpenHandkerchiefObj.transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.HandkerchiefMaterials [handkerchiefCo];
		}

		foreach (Transform child in assetsList.ClosedHandkerchiefObj.transform) {

			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
			skinRend.material = assetsList.HandkerchiefMaterials [handkerchiefCo];
		}

		//HairA==========================
		foreach (Transform child in assetsList.HairObjects [0].transform) {

			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();

			skinRend.material = assetsList.HairA_Materials [hairCo];

		}

		//HairB==========================
		foreach (Transform child in assetsList.HairObjects [1].transform) {
			
			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();

			skinRend.material = assetsList.HairB_Materials [hairCo];

		}

		//HairC==========================
		foreach (Transform child in assetsList.HairObjects [2].transform) {

			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();

			skinRend.material = assetsList.HairC_Materials [hairCo];

		}

		//HairD==========================
		foreach (Transform child in assetsList.HairObjects [3].transform) {

			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();

			skinRend.material = assetsList.HairD_Materials [hairCo];

		}

		//HairE==========================
		foreach (Transform child in assetsList.HairObjects [4].transform) {

			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();

			skinRend.material = assetsList.HairE_Materials [hairCo];

		}

		//HairF==========================
		foreach (Transform child in assetsList.HairObjects [5].transform) {

			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();

			skinRend.material = assetsList.HairF_Materials [hairCo];

		}

		//HairG==========================
		foreach (Transform child in assetsList.HairObjects [6].transform) {

			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();

			skinRend.material = assetsList.HairG_Materials [hairCo];

		}

		//HairH==========================
		foreach (Transform child in assetsList.HairObjects [7].transform) {

			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();

			skinRend.material = assetsList.HairH_Materials [hairCo];

		}

		//HairI==========================
		foreach (Transform child in assetsList.HairObjects [8].transform) {

			Renderer skinRend = child.gameObject.GetComponent<Renderer> ();

			skinRend.material = assetsList.HairI_Materials [hairCo];

		}






		assetsList.BeltObj.SetActive (false);
		assetsList.ButterflyTieObj.SetActive (false);
		assetsList.ClosedJacketObj.SetActive (false);
		assetsList.ClosedShirtObj.SetActive (false);
		assetsList.ClosedTieObj.SetActive (false);
		assetsList.ClosedWaistcoatObj.SetActive (false);
		assetsList.OpenJacketObj.SetActive (false);
		assetsList.OpenShirtObj.SetActive (false);
		assetsList.OpenTieWObj.SetActive (false);
		assetsList.OpenWaistcoatObj.SetActive (false);

		assetsList.PantsLoopsObj.SetActive (false);
		assetsList.ShirtObj.SetActive (false);
		assetsList.ShirtWObj.SetActive (false);

		assetsList.TieObj.SetActive (false);
		assetsList.WaistcoatObj.SetActive (false);
		assetsList.OpenHandkerchiefObj.SetActive (false);
		assetsList.ClosedHandkerchiefObj.SetActive (false);

		//Set hair
		for (int i = 0; i < assetsList.HairObjects.Length; i++) {
			assetsList.HairObjects [i].SetActive (false);
		}
		assetsList.HairObjects [hairTy].SetActive (true);

		//Set glasses
		for (int i = 0; i < assetsList.GlassesObjects.Length; i++) {
			assetsList.GlassesObjects [i].SetActive (false);
		}
		if (glassesTy > 0) {
			assetsList.GlassesObjects [faceTy].SetActive (true);
			foreach (Transform child in assetsList.GlassesObjects [faceTy].transform) {
				
				Renderer skinRend = child.gameObject.GetComponent<Renderer> ();

				mat = new Material[2];
				mat [0] = assetsList.Glass_Materials [0];
				mat [1] = assetsList.Glass_Materials [glassesTy];
				skinRend.materials = mat;
			}





		}
		// set face
		for (int i = 0; i < assetsList.FaceObjects.Length; i++) {
			assetsList.FaceObjects [i].SetActive (false);
		}
		assetsList.FaceObjects [faceTy].SetActive (true);
		foreach (Transform child in assetsList.FaceObjects [faceTy].transform) {
			
			string oName = child.gameObject.name;
			string hName = oName.Substring (0, 1);

			if (hName == "F") { 
				Renderer skinRend = child.gameObject.GetComponent<Renderer> ();
				skinRend.material = assetsList.Skin_Materials [skinTy];
			}
		}
		if (beltTy == 0) {
			assetsList.BeltObj.SetActive (true);
		}


		if (jacketTy == 0) {
			assetsList.OpenJacketObj.SetActive (true);
			assetsList.OpenShirtObj.SetActive (true);
			assetsList.OpenHandkerchiefObj.SetActive (true);
			if (tieTy == 0) {
				assetsList.TieObj.SetActive (true);

			}
			if (waistcoatTy == 0) {
				assetsList.OpenWaistcoatObj.SetActive (true);
				assetsList.BeltObj.SetActive (false);
				if (tieTy == 0) {
					assetsList.TieObj.SetActive (false);
					assetsList.ClosedTieObj.SetActive (true);
				}
			}

		} else if (jacketTy == 1) {
			
			assetsList.ClosedJacketObj.SetActive (true);
			assetsList.ClosedShirtObj.SetActive (true);
			assetsList.ClosedHandkerchiefObj.SetActive (true);
			assetsList.BeltObj.SetActive (false);
			if (tieTy == 0) {
				assetsList.TieObj.SetActive (false);
				assetsList.OpenTieWObj.SetActive (true);
			}
			if (waistcoatTy == 0) {
				assetsList.BeltObj.SetActive (false);
				assetsList.ClosedWaistcoatObj.SetActive (true);
			}
		} else if (jacketTy == 2) {
			assetsList.ShirtObj.SetActive (true);
			assetsList.PantsLoopsObj.SetActive (true);
			if (tieTy == 0) {
				assetsList.TieObj.SetActive (true);

			}
			if (waistcoatTy == 0) {
				assetsList.BeltObj.SetActive (false);
				assetsList.ShirtObj.SetActive (false);
				assetsList.ShirtWObj.SetActive (true);
				assetsList.WaistcoatObj.SetActive (true);
				assetsList.OpenTieWObj.SetActive (true);
				assetsList.TieObj.SetActive (false);
				assetsList.PantsLoopsObj.SetActive (false);
			}

		}

		if (tieTy == 1) {
			assetsList.ButterflyTieObj.SetActive (true);
			assetsList.OpenTieWObj.SetActive (false);
		}
		if (tieTy == 2) {
			assetsList.ButterflyTieObj.SetActive (false);
			assetsList.OpenTieWObj.SetActive (false);
		}
		if (handkerchiefTy == 1) {
			assetsList.OpenHandkerchiefObj.SetActive (false);
			assetsList.ClosedHandkerchiefObj.SetActive (false);
		}





	}

	void OnValidate ()
	{
		//code for In Editor customize
		hairC = (int)hairCol;
		eyeC = (int)eyeCol;
		glassesT = (int)glasses;
		hairT = (int)hair;
		faceT = (int)faceType;
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

		charCustomize (faceT, skinT, eyeC, glassesT, hairT, hairC, jacketT, waistcoatT, tieT, beltT, handkerchiefT, jacketC, shirtC, waistcoatC, tieC, pantsC, shoesC, handkerchiefC);

	}

}
