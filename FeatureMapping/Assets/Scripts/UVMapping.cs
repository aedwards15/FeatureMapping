using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class UVMapping : MonoBehaviour {

    #region INITIALIZERS

    public Mesh TheMesh;
	public Mesh EyeMesh;
	public Mesh ShirtMesh;
	public Mesh PantMesh;
	public GameObject adjuster;
	public Rect EyeLeft = new Rect();
	public Rect EyeRight = new Rect ();
	public Rect Nose = new Rect ();
	public Rect Mouth = new Rect();
	public Rect Face = new Rect();

	public int ImageWidth = 0;
	public int ImageHeight = 0;

	public bool isFullBody = false;

	public String CoordinatesFileName = "MyFile.txt";
	private String facialFeaturesFileName;

	//private Mesh theMesh;
	private Vector2[] theOldUvs;
	private Vector2[] theUVs;
	private Vector2[] eyesUVs;
	private Vector2[] shirtUVs;
	private Vector2[] pantsUVs;

	List<VectorI> mainBodyArray;
	List<VectorI> altBodyArray;
	List<VectorI> rightEyeArray;
	List<VectorI> leftEyeArray;
	List<VectorI> mainShirtArray;
	List<VectorI> altShirtArray;
	List<VectorI> mainPantsArray;
	List<VectorI> altPantsArray;

    enum state
    {
        Body,
        Face,
        Eyes,
        Shirt,
        Pants
    }

    state currentState = state.Body;

	[System.Serializable]
	private class VectorI
	{
		public Vector3 VectorRealWorld
		{
			get;
			set;
		}

		public Vector3 VectorLocal
		{
			get;
			set;
		}

		public int Index
		{
			get;
			set;
		}

		public VectorI(int index, Vector3 vectorLocal)
		{
			Index = index;
			VectorLocal = vectorLocal;
		}

		public VectorI(int index, Vector3 vectorWorld, Vector3 vectorLocal)
		{
			Index = index;
			VectorRealWorld = vectorWorld;
			VectorLocal = vectorLocal;
		}
	}

    #endregion

    #region START

    int noseIndex = -1;
	int x0Index = -1;
	int x1Index = -1;
	int y0Index = -1;
	int y1Index = -1;

	Vector3 origRight = new Vector3();
    float rightMod = 0f;
	Vector3 origLeft = new Vector3();
    float leftMod = 0f;
    Vector3 origTop = new Vector3();
    float topMod = 0f;
    Vector3 origBottom = new Vector3();
    float bottomMod = 0f;

    private GameObject left;
	private GameObject right;
	private GameObject top;
	private GameObject bottom;
	float modelWidth = 0f;
	float modelHeight = 0f;
	Text txt;
	Text txt2;
	void Start()
	{
		facialFeaturesFileName = "FacialFeatures.txt";
		if( TheMesh == null )
		{
			TheMesh = this.gameObject.GetComponent<MeshFilter>().mesh;
		}

		if( EyeMesh == null )
		{
			EyeMesh = GameObject.FindGameObjectWithTag( "Eyes" ).GetComponent<MeshFilter>().mesh;
		}

		if( ShirtMesh == null )
		{
			ShirtMesh = GameObject.FindGameObjectWithTag( "Shirt" ).GetComponent<MeshFilter>().mesh;
		}

		if( PantMesh == null )
		{
			PantMesh = GameObject.FindGameObjectWithTag( "Pants" ).GetComponent<MeshFilter>().mesh;
		}

        gameCamera = GameObject.FindGameObjectWithTag("MainCamera");
        playerModel = GameObject.FindGameObjectWithTag("Player");

        playerModel.transform.position = new Vector3(0.0f, -2.19f, -4.82f);

        /*int indexLeft = 0;
		int indexRight = 0;
		int indexTop = 0;
		int indexBottom = 0;

		for( int i = 0; i < TheMesh.vertexCount; i++ )
		{
			if( TheMesh.vertices [i].x >= TheMesh.vertices [indexRight].x )
				indexRight = i;

			if( TheMesh.vertices [i].x <= TheMesh.vertices [indexLeft].x )
				indexLeft = i;

			if( TheMesh.vertices [i].y >= TheMesh.vertices [indexTop].y )
				indexRight = i;

			if( TheMesh.vertices [i].y <= TheMesh.vertices [indexRight].y )
				indexRight = i;
		}

		modelWidth = TheMesh.vertices[indexRight].x - TheMesh.vertices[indexLeft].x;
		modelHeight = TheMesh.vertices[indexTop].y - TheMesh.vertices[indexBottom].y;
		*/

        //txt = GameObject.Find( "Text" ).GetComponent<Text>();
        //txt2 = GameObject.Find( "Text2" ).GetComponent<Text>();

        theUVs = new Vector2[TheMesh.uv.Length];
		theUVs = TheMesh.uv;
		theOldUvs = new Vector2[TheMesh.uv.Length];
		theOldUvs = TheMesh.uv;
		eyesUVs = new Vector2[EyeMesh.uv.Length];
		eyesUVs = EyeMesh.uv;
		shirtUVs = new Vector2[ShirtMesh.uv.Length];
		shirtUVs = ShirtMesh.uv;
		pantsUVs = new Vector2[PantMesh.uv.Length];
		pantsUVs = PantMesh.uv;

		verticeSpheres = new List<GameObject> ();

		mainBodyArray = new List<VectorI> ();

		//TextAsset headVerticesText = (TextAsset)Resources.Load( "HeadVertices" );

		//if( headVerticesText != null && !String.IsNullOrEmpty( headVerticesText.text ) )
		//{

		//if( File.Exists( Application.persistentDataPath + "/HeadVertices.txt" ) )
		//{
		//	File.Delete( Application.persistentDataPath + "/HeadVertices.txt" );
		//}

		if( File.Exists( Application.persistentDataPath + "/HeadVertices.txt" ) )
		{
			String temp = Application.persistentDataPath + "/HeadVertices.txt";
			var sr = File.OpenText( Application.persistentDataPath + "/HeadVertices.txt" );
			String headVerticesText = sr.ReadToEnd();
			String[] parse = headVerticesText.Split( new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries );

			noseIndex = Convert.ToInt32( parse[0].Split( '=' ) [1] );
            Instantiate(adjuster, transform.TransformPoint(TheMesh.vertices[noseIndex]), Quaternion.identity);

            x0Index = Convert.ToInt32( parse[1].Split( '=' ) [1] );
            Instantiate(adjuster, transform.TransformPoint(TheMesh.vertices[x0Index]), Quaternion.identity);

			x1Index = Convert.ToInt32( parse[2].Split( '=' ) [1] );
            Instantiate(adjuster, transform.TransformPoint(TheMesh.vertices[x1Index]), Quaternion.identity);

            y0Index = Convert.ToInt32( parse[3].Split( '=' ) [1] );
            Instantiate(adjuster, transform.TransformPoint(TheMesh.vertices[y0Index]), Quaternion.identity);

            y1Index = Convert.ToInt32( parse[4].Split( '=' ) [1] );
            Instantiate(adjuster, transform.TransformPoint(TheMesh.vertices[y1Index]), Quaternion.identity);

            for ( int i = 5; i < parse.Length; i++ )
			{
				int index = Convert.ToInt32( parse [i].Split( ',' ) [0] );
				mainBodyArray.Add( new VectorI (index, TheMesh.vertices [index]) );
			}

			sr = File.OpenText( Application.persistentDataPath + "/BodyVertices.txt" );
			String bodyVerticesText = sr.ReadToEnd();
			parse = bodyVerticesText.Split( new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries );

            altBodyArray = new List<VectorI>();

            for ( int i = 0; i < parse.Length; i++ )
			{
				int index = Convert.ToInt32( parse [i].Split( ',' ) [0] );
				altBodyArray.Add( new VectorI (index, transform.TransformPoint( TheMesh.vertices [index] ), TheMesh.vertices [index]) );
			}

			/*sr = File.OpenText( Application.persistentDataPath + "/EyeVertices.txt" );
			String eyeVerticesText = sr.ReadToEnd();
			parse = bodyVerticesText.Split( new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries );
			x0RightEye = Convert.ToInt32(parse [0]);
			x1RightEye = Convert.ToInt32(parse [1]);
			y1RightEye = Convert.ToInt32(parse [2]);
			y0RightEye = Convert.ToInt32(parse [3]);
			x0LeftEye = Convert.ToInt32(parse [4]);
			x1LeftEye = Convert.ToInt32(parse [5]);
			y1LeftEye = Convert.ToInt32(parse [6]);
			y0LeftEye = Convert.ToInt32(parse [7]);

			TextAsset shirtVerticesText = (TextAsset)Resources.Load( "ShirtVertices" );

			parse = shirtVerticesText.text.Split( new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries );

			for( int i = 0; i < parse.Length; i++ )
			{
				int index = Convert.ToInt32( parse [i].Split( ',' ) [0] );
				mainShirtArray.Add( new VectorI (index, transform.TransformPoint( TheMesh.vertices [index] ), TheMesh.vertices [index]) );
			}

			TextAsset altShirtVerticesText = (TextAsset)Resources.Load( "AltShirtVertices" );

			parse = altShirtVerticesText.text.Split( new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries );

			for( int i = 0; i < parse.Length; i++ )
			{
				int index = Convert.ToInt32( parse [i].Split( ',' ) [0] );
				altShirtArray.Add( new VectorI (index, transform.TransformPoint( TheMesh.vertices [index] ), TheMesh.vertices [index]) );
			}

			TextAsset pantsVerticesText = (TextAsset)Resources.Load( "PantsVertices" );

			parse = pantsVerticesText.text.Split( new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries );

			for( int i = 0; i < parse.Length; i++ )
			{
				int index = Convert.ToInt32( parse [i].Split( ',' ) [0] );
				mainPantsArray.Add( new VectorI (index, transform.TransformPoint( TheMesh.vertices [index] ), TheMesh.vertices [index]) );
			}

			TextAsset altPantsVerticesText = (TextAsset)Resources.Load( "AltPantsVertices" );

			parse = altPantsVerticesText.text.Split( new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries );

			for( int i = 0; i < parse.Length; i++ )
			{
				int index = Convert.ToInt32( parse [i].Split( ',' ) [0] );
				altPantsArray.Add( new VectorI (index, transform.TransformPoint( TheMesh.vertices [index] ), TheMesh.vertices [index]) );
			}
            */
		}
		else
		{
			LoadValues();
		}

        right = GameObject.FindGameObjectWithTag("Right");
        origRight = right.transform.position;
        right.SetActive(false);

        left = GameObject.FindGameObjectWithTag("Left");
        origLeft = left.transform.position;
        left.SetActive(false);

        bottom = GameObject.FindGameObjectWithTag("Bottom");
        origBottom = bottom.transform.position;
        bottom.SetActive(false);

        top = GameObject.FindGameObjectWithTag("Top");
        origTop = top.transform.position;
        top.SetActive(false);

        /*if( File.Exists( CoordinatesFileName ) )
		{
			var sr = File.OpenText( CoordinatesFileName );
			var line = sr.ReadLine();
			line = sr.ReadLine();
			first = new Vector2 (Convert.ToSingle(line.Split( ',' ) [0]), Convert.ToSingle(line.Split( ',' ) [1]));
			line = sr.ReadLine();
			second = new Vector2 (Convert.ToSingle(line.Split( ',' ) [0]), Convert.ToSingle(line.Split( ',' ) [1]));
			line = sr.ReadLine();
			line = sr.ReadLine();
			line = sr.ReadLine();
			while( line != null )
			{
				int index = Convert.ToInt32( line.Split( ',' ) [0] );
				vertexArray.Add( new VectorI (index, transform.TransformPoint( TheMesh.vertices [index] ), TheMesh.vertices [index]) );
				line = sr.ReadLine();
			}  
		}*/

        /*if( File.Exists( facialFeaturesFileName ) )
		{
			var sr = File.OpenText( facialFeaturesFileName );
			var line = sr.ReadLine();
			noseIndex = Convert.ToInt32( line.Split( '=' ) [1] );
			line = sr.ReadLine();
			x0Index = Convert.ToInt32( line.Split( '=' ) [1] );
			right = (GameObject)Instantiate( sphere, new Vector3 (transform.TransformPoint(TheMesh.vertices[x0Index]).x, 
				transform.TransformPoint(TheMesh.vertices[x0Index]).y, -9.4f), Quaternion.identity );
			line = sr.ReadLine();
			x1Index = Convert.ToInt32( line.Split( '=' ) [1] );
			left = (GameObject)Instantiate( sphere, new Vector3 (transform.TransformPoint(TheMesh.vertices[x1Index]).x, 
				transform.TransformPoint(TheMesh.vertices[x1Index]).y, -9.4f), Quaternion.identity );
			line = sr.ReadLine();
			y0Index = Convert.ToInt32( line.Split( '=' ) [1] );
			bottom = (GameObject)Instantiate( sphere, new Vector3 (transform.TransformPoint(TheMesh.vertices[y0Index]).x, 
				transform.TransformPoint(TheMesh.vertices[y0Index]).y, -9.4f), Quaternion.identity );
			line = sr.ReadLine();
			y1Index = Convert.ToInt32( line.Split( '=' ) [1] );
			top = (GameObject)Instantiate( sphere, new Vector3 (transform.TransformPoint(TheMesh.vertices[y1Index]).x, 
				transform.TransformPoint(TheMesh.vertices[y1Index]).y, -9.4f), Quaternion.identity );

			modelWidth = right.transform.position.x - left.transform.position.x;
			modelHeight = top.transform.position.y - bottom.transform.position.y;
		}*/
    }

	void LoadValues()
	{
		FindHeadValues();

		FindEyeValues();

		FindShirtValues();

		FindPantsValues();
	}

	void FindHeadValues()
	{
		List<VectorI> tempVerts = new List<VectorI>();

		for( int i = 0; i < TheMesh.vertices.Length; i++ )
		{
			mainBodyArray.Add( new VectorI (i, TheMesh.vertices [i]) );
		}

		VectorI[] temp1 = new VectorI[TheMesh.vertices.Length];

		mainBodyArray = ( from vert in mainBodyArray
			select vert ).OrderBy( y => y.VectorLocal.y ).ToList();

		mainBodyArray.CopyTo( 0, temp1, 0, (int)( mainBodyArray.Count / 1.57f ));

		mainBodyArray.RemoveRange( 0, (int)( mainBodyArray.Count / 1.57f ) );

		mainBodyArray = ( from vert in mainBodyArray
			select vert ).OrderBy( y => y.VectorLocal.z ).ToList();

		mainBodyArray.CopyTo( 0, temp1, (int)(TheMesh.vertices.Length / 1.57f ), (int)( mainBodyArray.Count / 1.5f ) );

		mainBodyArray.RemoveRange( 0, (int)( mainBodyArray.Count / 1.5f ) );

		VectorI temp = ( from vert in mainBodyArray
			select vert ).OrderBy( y => y.VectorLocal.x ).Take( 1 ).ToList()[0];
		x0Index = temp.Index;

		temp = ( from vert in mainBodyArray
			select vert ).OrderByDescending( y => y.VectorLocal.x ).Take( 1 ).ToList()[0];
		x1Index = temp.Index;

		temp = ( from vert in mainBodyArray
			select vert ).OrderBy( y => y.VectorLocal.y ).Take( 1 ).ToList()[0];
		y0Index = temp.Index;

		temp = ( from vert in mainBodyArray
			select vert ).OrderByDescending( y => y.VectorLocal.y ).Take( 1 ).ToList()[0];
		y1Index = temp.Index;

		temp = ( from vert in mainBodyArray
			select vert ).OrderByDescending( y => y.VectorLocal.z ).Take( 1 ).ToList()[0];
		noseIndex = temp.Index;

		altBodyArray = new List<VectorI> ();
		altBodyArray = ( from vert in temp1
			where vert != null
			select vert).ToList();

		if( File.Exists( Application.persistentDataPath + "/HeadVertices.txt" ) )
		{
			File.Delete( Application.persistentDataPath + "/HeadVertices.txt" );
		}

		var sr = File.CreateText( Application.persistentDataPath + "/HeadVertices.txt" );
		sr.WriteLine( String.Format( "nose={0}", noseIndex ) );
		sr.WriteLine( String.Format( "right={0}", x0Index ) );
		sr.WriteLine( String.Format( "left={0}", x1Index ) );
		sr.WriteLine( String.Format( "bottom={0}", y0Index ) );
		sr.WriteLine( String.Format( "top={0}", y1Index ) );
		foreach( var item in mainBodyArray )
		{
			sr.WriteLine( String.Format( "{0},{1},{2},{3}", item.Index, item.VectorLocal.x, item.VectorLocal.y, item.VectorLocal.z ) );
		}
		sr.Close();

		if( File.Exists( Application.persistentDataPath + "/BodyVertices.txt" ) )
		{
			File.Delete( Application.persistentDataPath + "/BodyVertices.txt" );
		}

		sr = File.CreateText( Application.persistentDataPath + "/BodyVertices.txt" );
		foreach( var item in altBodyArray )
		{
			sr.WriteLine( String.Format( "{0},{1},{2},{3}", item.Index, item.VectorLocal.x, item.VectorLocal.y, item.VectorLocal.z ) );
		}
		sr.Close();
	}

	int x0RightEye;
	int x1RightEye;
	int y0RightEye;
	int y1RightEye;
	int x0LeftEye;
	int x1LeftEye;
	int y0LeftEye;
	int y1LeftEye;
	List<VectorI> rightVerts;
	List<VectorI> leftVerts;
	void FindEyeValues()
	{
		rightVerts = new List<VectorI>();

		for( int i = 0; i < EyeMesh.vertices.Length; i++ )
		{
			rightVerts.Add( new VectorI (i, EyeMesh.vertices [i]) );
		}

		leftVerts = new List<VectorI> ();

		rightVerts = ( from vert in rightVerts
		               select vert ).OrderBy( y => y.VectorLocal.x ).ToList();

		leftVerts = ( from vert in rightVerts
		             select vert ).OrderByDescending( y => y.VectorLocal.x ).ToList();

		rightVerts.RemoveRange( 0, rightVerts.Count / 2 );

		leftVerts.RemoveRange( 0, leftVerts.Count / 2 );

		rightVerts = ( from vert in rightVerts
			select vert ).OrderBy( y => y.VectorLocal.z ).ToList();

		leftVerts = ( from vert in leftVerts
			select vert ).OrderBy( y => y.VectorLocal.z ).ToList();

		rightVerts.RemoveRange( 0, (int)(rightVerts.Count / 2f) );

		leftVerts.RemoveRange( 0, (int)(leftVerts.Count / 2f) );

		VectorI temp = ( from vert in rightVerts
		                 select vert ).OrderByDescending( y => y.VectorLocal.x ).Take( 1 ).ToList() [0];
		x0RightEye = temp.Index;

		temp = ( from vert in rightVerts
		         select vert ).OrderBy( y => y.VectorLocal.x ).Take( 1 ).ToList() [0];
		x1RightEye = temp.Index;

		temp = ( from vert in rightVerts
		         select vert ).OrderBy( y => y.VectorLocal.y ).Take( 1 ).ToList() [0];
		y0RightEye = temp.Index;

		temp = ( from vert in rightVerts
		         select vert ).OrderByDescending( y => y.VectorLocal.y ).Take( 1 ).ToList() [0];
		y1RightEye = temp.Index;

		temp = ( from vert in leftVerts
			select vert ).OrderByDescending( y => y.VectorLocal.x ).Take( 1 ).ToList() [0];
		x0LeftEye = temp.Index;

		temp = ( from vert in leftVerts
			select vert ).OrderBy( y => y.VectorLocal.x ).Take( 1 ).ToList() [0];
		x1LeftEye = temp.Index;

		temp = ( from vert in leftVerts
			select vert ).OrderBy( y => y.VectorLocal.y ).Take( 1 ).ToList() [0];
		y0LeftEye = temp.Index;

		temp = ( from vert in leftVerts
			select vert ).OrderByDescending( y => y.VectorLocal.y ).Take( 1 ).ToList() [0];
		y1LeftEye = temp.Index;

		if( File.Exists( Application.persistentDataPath + "/EyeVertices.txt" ) )
		{
			File.Delete( Application.persistentDataPath + "/EyeVertices.txt" );
		}

		var sr = File.CreateText( Application.persistentDataPath + "/EyeVertices.txt" );
		sr.WriteLine( String.Format( "rightEyeRight={0}", x0RightEye ) );
		sr.WriteLine( String.Format( "rightEyeLeft={0}", x1RightEye ) );
		sr.WriteLine( String.Format( "rightEyeTop={0}", y1RightEye ) );
		sr.WriteLine( String.Format( "rightEyeBottom={0}", y0RightEye ) );
		sr.WriteLine( String.Format( "leftEyeRight={0}", x0LeftEye ) );
		sr.WriteLine( String.Format( "leftEyeLeft={0}", x1LeftEye ) );
		sr.WriteLine( String.Format( "leftEyeTop={0}", y1LeftEye ) );
		sr.WriteLine( String.Format( "leftEyeBottom={0}", y0LeftEye ) );
		sr.Close();
	}

	int x0Shirt;
	int x1Shirt;
	int y0Shirt;
	int y1Shirt;
	void FindShirtValues()
	{
		mainShirtArray = new List<VectorI> ();
		altShirtArray = new List<VectorI> ();
		List<VectorI> tempVerts = new List<VectorI>();

		for( int i = 0; i < ShirtMesh.vertices.Length; i++ )
		{
			mainShirtArray.Add( new VectorI (i, ShirtMesh.vertices [i]) );
		}

		/*mainShirtArray = ( from vert in mainShirtArray
			select vert ).OrderBy( y => y.VectorLocal.z ).ToList();

		altShirtArray = ( from vert in mainShirtArray
			select vert ).OrderByDescending( y => y.VectorLocal.z ).ToList();

		mainShirtArray.RemoveRange( 0, mainShirtArray.Count / 2 );

		altShirtArray.RemoveRange( 0, altShirtArray.Count / 2 );*/

		VectorI temp = ( from vert in mainShirtArray
			select vert ).OrderBy( y => y.VectorLocal.x ).Take( 1 ).ToList() [0];
		x0Shirt = temp.Index;

		temp = ( from vert in mainShirtArray
			select vert ).OrderByDescending( y => y.VectorLocal.x ).Take( 1 ).ToList() [0];
		x1Shirt = temp.Index;

		temp = ( from vert in mainShirtArray
			select vert ).OrderBy( y => y.VectorLocal.y ).Take( 1 ).ToList() [0];
		y0Shirt = temp.Index;

		temp = ( from vert in mainShirtArray
			select vert ).OrderByDescending( y => y.VectorLocal.y ).Take( 1 ).ToList() [0];
		y1Shirt = temp.Index;
	}

	int x0Pants;
	int x1Pants;
	int y0Pants;
	int y1Pants;
	void FindPantsValues()
	{
		mainPantsArray = new List<VectorI> ();
		altPantsArray = new List<VectorI> ();
		List<VectorI> tempVerts = new List<VectorI>();

		for( int i = 0; i < PantMesh.vertices.Length; i++ )
		{
			mainPantsArray.Add( new VectorI (i, PantMesh.vertices [i]) );
		}

		/*mainShirtArray = ( from vert in mainShirtArray
			select vert ).OrderBy( y => y.VectorLocal.z ).ToList();

		altShirtArray = ( from vert in mainShirtArray
			select vert ).OrderByDescending( y => y.VectorLocal.z ).ToList();

		mainShirtArray.RemoveRange( 0, mainShirtArray.Count / 2 );

		altShirtArray.RemoveRange( 0, altShirtArray.Count / 2 );*/

		VectorI temp = ( from vert in mainPantsArray
			select vert ).OrderBy( y => y.VectorLocal.x ).Take( 1 ).ToList() [0];
		x0Pants = temp.Index;

		temp = ( from vert in mainPantsArray
			select vert ).OrderByDescending( y => y.VectorLocal.x ).Take( 1 ).ToList() [0];
		x1Pants = temp.Index;

		temp = ( from vert in mainPantsArray
			select vert ).OrderBy( y => y.VectorLocal.y ).Take( 1 ).ToList() [0];
		y0Pants = temp.Index;

		temp = ( from vert in mainPantsArray
			select vert ).OrderByDescending( y => y.VectorLocal.y ).Take( 1 ).ToList() [0];
		y1Pants = temp.Index;
	}
    #endregion

    #region LOAD

    private List<GameObject> verticeSpheres;
	Vector2 firstValue = new Vector2();
	Vector2 secondValue = new Vector2();

	private Vector3 screenPoint;
	private Vector3 offset;

	private GameObject firstPoint;
	private GameObject secondPoint;

	private GameObject selectedPoint;

    private GameObject gameCamera;
    private GameObject playerModel;

    void Update()
	{
		if( Input.GetKeyUp( KeyCode.UpArrow ) )
		{
			//if (vertexArray.Count > 0)
			//	verticeSpheres.Add( (GameObject)Instantiate( sphere, new Vector3 (vertexArray [verticeSpheres.Count].VectorRealWorld.x, vertexArray [verticeSpheres.Count].VectorRealWorld.y, -9.4f), Quaternion.identity ) );

			if( Input.GetKey( KeyCode.LeftShift ) )
			{
				bottom.transform.position = new Vector3 (bottom.transform.position.x, ( bottom.transform.position.y + ( modelHeight * .025f ) ), bottom.transform.position.z);
			}
			else
			{
				top.transform.position = new Vector3 (top.transform.position.x, ( top.transform.position.y + ( modelHeight * .025f ) ), top.transform.position.z);
			}
		}
		else if( Input.GetKeyUp( KeyCode.DownArrow ) )
		{
			//if( verticeSpheres.Count > 0 )
			//{
			//	Destroy( verticeSpheres [verticeSpheres.Count - 1] );
			//	verticeSpheres.RemoveAt( verticeSpheres.Count - 1 );
			//}

			if( Input.GetKey( KeyCode.LeftShift ) )
			{
				bottom.transform.position = new Vector3 (bottom.transform.position.x, ( bottom.transform.position.y - ( modelHeight * .025f ) ), bottom.transform.position.z);
			}
			else
			{
				top.transform.position = new Vector3 (top.transform.position.x, ( top.transform.position.y - ( modelHeight * .025f ) ), top.transform.position.z);
			}
		}
		else if( Input.GetKeyUp( KeyCode.LeftArrow ) )
		{
			if( Input.GetKey( KeyCode.LeftShift ) )
			{
				left.transform.position = new Vector3 (( left.transform.position.x + ( modelWidth * .025f ) ), left.transform.position.y, left.transform.position.z);
			}
			else
			{
				right.transform.position = new Vector3 (( right.transform.position.x + ( modelWidth * .025f ) ), right.transform.position.y, right.transform.position.z);
			}
		}
		else if( Input.GetKeyUp( KeyCode.RightArrow ) )
		{
			if( Input.GetKey( KeyCode.LeftShift ) )
			{
				left.transform.position = new Vector3 (( left.transform.position.x - ( modelWidth * .025f ) ), left.transform.position.y, left.transform.position.z);
			}
			else
			{
				right.transform.position = new Vector3 (( right.transform.position.x - ( modelWidth * .025f ) ), right.transform.position.y, right.transform.position.z);
			}
		}

		{
			if( Input.GetMouseButtonDown( 0 ) )
			{
				Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );
				RaycastHit hit;

				if( Physics.Raycast( ray, out hit ) )
				{
					if( currentState != state.Body && hit.transform.gameObject.transform.parent.tag != "Player")
					{
                        selectedPoint = hit.transform.gameObject;

                        screenPoint = Camera.main.WorldToScreenPoint(selectedPoint.transform.position);
                        offset = selectedPoint.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
                    }
                    else if (hit.transform.gameObject.tag == "Head")
                    {
                        playerModel.transform.position = new Vector3(0.0f, -3.84f, -8.74f);
                        currentState = state.Face;

                        left.SetActive(true);
                        right.SetActive(true);
                        top.SetActive(true);
                        bottom.SetActive(true);
                    }
                    else if (hit.transform.gameObject.tag == "Eyes")
                    {
                        //playerModel.transform.position = new Vector3(0.0f, -3.84f, -8.74f);
                        currentState = state.Eyes;
                    }
                    else if (hit.transform.gameObject.tag == "Shirt")
                    {
                        playerModel.transform.position = new Vector3(0.0f, -2.944f, -7.549f);
                        currentState = state.Shirt;
                    }
                    else if (hit.transform.gameObject.tag == "Pants")
                    {
                        playerModel.transform.position = new Vector3(0.0f, -1.342f, -7.752f);
                        currentState = state.Pants;
                    }
                }
			}
			else if( Input.GetMouseButton( 0 ) )
			{
				if( selectedPoint != null )
				{
                    Vector3 cursorPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
                    Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(cursorPoint) + offset;

                    if (selectedPoint.tag == "Left" || selectedPoint.tag == "Right")
                    {
                        selectedPoint.transform.position = new Vector3(cursorPosition.x, selectedPoint.transform.position.y, selectedPoint.transform.position.z);
                    }
                    else
                    {
                        selectedPoint.transform.position = new Vector3(selectedPoint.transform.position.x, cursorPosition.y, selectedPoint.transform.position.z);
                    }

                    SetTheUVs();
				}
			}
			else if( Input.GetMouseButtonUp( 0 ) )
			{
				if( selectedPoint != null )
				{
                    if (selectedPoint.tag == "Left")
                    {
                        leftMod += selectedPoint.transform.position.x - origLeft.x;
                        selectedPoint.transform.position = origLeft;
                    }
                    else if (selectedPoint.tag == "Right")
                    {
                        rightMod += selectedPoint.transform.position.x - origRight.x;
                        selectedPoint.transform.position = origRight;
                    }
                    else if (selectedPoint.tag == "Top")
                    {
                        topMod += selectedPoint.transform.position.y - origTop.y;
                        selectedPoint.transform.position = origTop;
                    }
                    else
                    {
                        bottomMod += selectedPoint.transform.position.y - origBottom.y;
                        selectedPoint.transform.position = origBottom;
                    }

                    selectedPoint = null;
				}
				else
				{
					
				}
			}
		}

		if( Input.touchCount > 0)
		{
            if (Input.GetTouch(0).phase == TouchPhase.Began)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (currentState != state.Body && hit.transform.gameObject.transform.parent.tag != "Player")
                    {
                        selectedPoint = hit.transform.gameObject;

                        screenPoint = Camera.main.WorldToScreenPoint(selectedPoint.transform.position);
                        offset = selectedPoint.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, screenPoint.z));
                    }
                    else if (hit.transform.gameObject.tag == "Head")
                    {
                        playerModel.transform.position = new Vector3(0.0f, -3.84f, -8.74f);
                        currentState = state.Face;

                        left.SetActive(true);
                        right.SetActive(true);
                        top.SetActive(true);
                        bottom.SetActive(true);
                    }
                    else if (hit.transform.gameObject.tag == "Eyes")
                    {
                        //playerModel.transform.position = new Vector3(0.0f, -3.84f, -8.74f);
                        currentState = state.Eyes;
                    }
                    else if (hit.transform.gameObject.tag == "Shirt")
                    {
                        playerModel.transform.position = new Vector3(0.0f, -2.944f, -7.549f);
                        currentState = state.Shirt;
                    }
                    else if (hit.transform.gameObject.tag == "Pants")
                    {
                        playerModel.transform.position = new Vector3(0.0f, -1.342f, -7.752f);
                        currentState = state.Pants;
                    }
                }
            }
            else if (Input.GetTouch(0).phase == TouchPhase.Moved)
            {
                if (selectedPoint != null)
                {
                    Vector3 cursorPoint = new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, screenPoint.z);
                    Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(cursorPoint) + offset;

                    if (selectedPoint.tag == "Left" || selectedPoint.tag == "Right")
                    {
                        selectedPoint.transform.position = new Vector3(cursorPosition.x, selectedPoint.transform.position.y, selectedPoint.transform.position.z);
                    }
                    else
                    {
                        selectedPoint.transform.position = new Vector3(selectedPoint.transform.position.x, cursorPosition.y, selectedPoint.transform.position.z);
                    }

                    SetTheUVs();
                }
            }
            else if (Input.GetTouch(0).phase == TouchPhase.Ended)
            {
                if (selectedPoint != null)
                {
                    if (selectedPoint.tag == "Left")
                    {
                        leftMod += selectedPoint.transform.position.x - origLeft.x;
                        selectedPoint.transform.position = origLeft;
                    }
                    else if (selectedPoint.tag == "Right")
                    {
                        rightMod += selectedPoint.transform.position.x - origRight.x;
                        selectedPoint.transform.position = origRight;
                    }
                    else if (selectedPoint.tag == "Top")
                    {
                        topMod += selectedPoint.transform.position.y - origTop.y;
                        selectedPoint.transform.position = origTop;
                    }
                    else
                    {
                        bottomMod += selectedPoint.transform.position.y - origBottom.y;
                        selectedPoint.transform.position = origBottom;
                    }

                    selectedPoint = null;
                }
                else
                {

                }
            }
		}

		// change the UV settings in the Inspector, then click the left mouse button to view
		if( Input.GetMouseButtonUp( 0 ) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended) )
		{
			if( Input.GetKey( KeyCode.LeftShift ) )
			{
				//Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );
				//RaycastHit hit;

				Vector3 point = new Vector3 ();
				//if( Physics.Raycast( ray, out hit ) )
				//{
				float mouseX = Input.mousePosition.x;
				float mouseY = Input.mousePosition.y;

				point = Camera.main.ScreenToWorldPoint( new Vector3 (mouseX, mouseY, 0.0f) );//hit.distance) );

				if( firstPoint == null )
				{
					firstPoint = (GameObject)Instantiate( adjuster, new Vector3 (point.x, point.y, -9.4f), Quaternion.identity );
					secondPoint = (GameObject)Instantiate( adjuster, new Vector3 (point.x, point.y, -9.4f), Quaternion.identity );

					firstValue.x = point.x;
					firstValue.y = point.y;
				}
				else
				{
					secondValue.x = point.x; //secondPoint.transform.position.x;
					secondValue.y = point.y; //secondPoint.transform.position.y;

					first = firstPoint.transform.position;
					second = secondPoint.transform.position;

					CreateArray();

					Destroy( firstPoint );
					firstPoint = null;
					Destroy( secondPoint );
					secondPoint = null;
				}
			}
			else if( Input.GetKey( KeyCode.LeftControl ) )
			{
				SetOldUVs();
			}
			else if( Input.GetKey( KeyCode.RightShift ) )
			{
				SetUVs2();
			}
		}

		DisplayPosition();
	}
		
	void CreateArray()
	{
		mainBodyArray = new List<VectorI> ();
		Vector3 vectorOfClosestToA = new Vector3();
		float valueOfClosestToA = 1000f;
		Vector3 vectorOfClosestToB = new Vector3();
		float valueOfClosestToB = 1000f;

		int indexLeft = -1;
		int indexRight = -1;
		int indexTop = -1;
		int indexBottom = -1;
		int indexNose = -1;

		for( int i = 0; i < TheMesh.vertices.Length; i++ )
		{
			Vector3 meshPoint = transform.TransformPoint( TheMesh.vertices [i] );

			if( meshPoint.x >= firstValue.x && meshPoint.x <= secondValue.x && meshPoint.y >= firstValue.y && meshPoint.y <= secondValue.y) //&& meshPoint.z < -9.147f)
			{
				float resultA = Vector2.Distance( new Vector2 (meshPoint.x, meshPoint.y), firstValue );
				float resultB = Vector2.Distance( new Vector2 (meshPoint.x, meshPoint.y), secondValue );

				if( resultA < valueOfClosestToA )
				{
					valueOfClosestToA = resultA;
					vectorOfClosestToA = TheMesh.vertices[i];
				}

				if( resultB < valueOfClosestToB )
				{
					valueOfClosestToB = resultB;
					vectorOfClosestToB = TheMesh.vertices[i];
				}

				if( indexRight == -1 || TheMesh.vertices [i].x > TheMesh.vertices [indexRight].x )
				{
					indexRight = i;
				}

				if( indexLeft == -1 || TheMesh.vertices [i].x < TheMesh.vertices [indexLeft].x )
				{
					indexLeft = i;
				}

				if( indexTop == -1 || TheMesh.vertices [i].y > TheMesh.vertices [indexTop].y )
				{
					indexTop = i;
				}

				if( indexBottom == -1 || TheMesh.vertices [i].y < TheMesh.vertices [indexBottom].y )
				{
					indexBottom = i;
				}

				if( indexNose == -1 || TheMesh.vertices [i].z > TheMesh.vertices [indexNose].z )
				{
					indexNose = i;
				}

				mainBodyArray.Add( new VectorI (i, meshPoint, TheMesh.vertices [i]) );
			}
		}

		mainBodyArray = ( from vertex in mainBodyArray
		              select vertex ).OrderBy( x => x.VectorRealWorld.x ).ThenBy( y => y.VectorRealWorld.y ).ToList();

		first = new Vector2 (vectorOfClosestToA.x, vectorOfClosestToA.y);
		second = new Vector2 (vectorOfClosestToB.x, vectorOfClosestToB.y);

		{
			if( File.Exists( CoordinatesFileName ) )
			{
				File.Delete( CoordinatesFileName );
			}

			var sr = File.CreateText( CoordinatesFileName );
			sr.WriteLine( "First Point,Second Point" );
			sr.WriteLine( String.Format( "{0},{1}", vectorOfClosestToA.x, vectorOfClosestToA.y ) );
			sr.WriteLine( String.Format( "{0},{1}", vectorOfClosestToB.x, vectorOfClosestToB.y ) );
			sr.WriteLine();
			sr.WriteLine( "Index,World X,World Y,World Z,Local X,Local Y,Local Z" );
			foreach( var item in mainBodyArray )
			{
				sr.WriteLine( String.Format( "{0},{1},{2},{3},{4},{5},{6}", item.Index, item.VectorRealWorld.x, item.VectorRealWorld.y, item.VectorRealWorld.z, item.VectorLocal.x, item.VectorLocal.y, item.VectorLocal.z ) );
			}
			sr.Close();
		}

		{
			if( File.Exists( facialFeaturesFileName ) )
			{
				File.Delete( facialFeaturesFileName );
			}

			var st = File.CreateText( facialFeaturesFileName );
			st.WriteLine( String.Format( "nose={0}", indexNose ) );
			st.WriteLine( String.Format( "right={0}", indexRight ) );
			st.WriteLine( String.Format( "left={0}", indexLeft ) );
			st.WriteLine( String.Format( "bottom={0}", indexBottom ) );
			st.WriteLine( String.Format( "top={0}", indexTop ) );
			st.WriteLine( String.Format( "nose={0}", TheMesh.vertices [indexNose].z ) );
			st.WriteLine( String.Format( "right={0}", TheMesh.vertices [indexRight].x ) );
			st.WriteLine( String.Format( "left={0}", TheMesh.vertices [indexLeft].x ) );
			st.WriteLine( String.Format( "bottom={0}", TheMesh.vertices [indexBottom].y ) );
			st.WriteLine( String.Format( "top={0}", TheMesh.vertices [indexTop].y ) );
			st.Close();
		}
	}

	float distance = 1.0f;
	void DisplayPosition()
	{
		Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );
		RaycastHit hit;

		Vector3 point = new Vector3 ();
		if( Physics.Raycast( ray, out hit ) )
		{
			point = Camera.main.ScreenToWorldPoint( new Vector3 (Input.mousePosition.x, Input.mousePosition.y, hit.distance) );

			//txt.text = "(" + point.x.ToString() + "," + point.y.ToString() + ")";
		}

		Vector3 point2 = ray.origin + (ray.direction * distance); 
		//txt2.text = "(" + point2.x.ToString() + "," + point2.y.ToString() + ")";

		if( secondPoint != null)
		{
			Vector3 point3 = Camera.main.ScreenToWorldPoint( new Vector3 (Input.mousePosition.x, Input.mousePosition.y, 0.0f) );
			float y = firstPoint.transform.position.y + ( point3.x - firstPoint.transform.position.x );
			secondPoint.transform.position = new Vector3(point3.x, point3.y, -9.4f);
		}
	}
    #endregion

    #region EVENT

    private Vector2 first = new Vector2 ();
	private Vector2 second = new Vector2 ();

	void SetTheUVs()
	{
        if (currentState == state.Eyes)
            SetEyes();
        else if (currentState == state.Shirt)
            SetShirt();
        else if (currentState == state.Pants)
            SetPants();
        else
        {
            float s0 = EyeRight.x;
            float s1 = EyeLeft.x + EyeLeft.width;
            float t0 = Mouth.y;
            float t1 = EyeLeft.y + EyeLeft.height;

            //float altered1 = ( (( Face.x + Face.width ) + ( EyeLeft.x + EyeLeft.width ) ) / 2 );
            //float altered2 = (( Face.x + EyeRight.x ) / 2 );
            //float alteredFaceWidth = (altered1 - altered2);
            float dsx = ((s1 - s0) / ((TheMesh.vertices[x1Index].x - (left.transform.position.x + leftMod - origLeft.x)) -
                (TheMesh.vertices[x0Index].x - (right.transform.position.x + rightMod - origRight.x))));
            //float dsx = ((s1 - s0) / (TheMesh.vertices[x1Index].x - TheMesh.vertices[x0Index].x));
            //Vector3 vect = transform.TransformPoint(TheMesh.vertices[x0Index]);
            //vect.z = 0.5f;
            //GameObject temp0 = (GameObject)Instantiate(sphere, vect, Quaternion.identity);
            //vect = transform.TransformPoint(TheMesh.vertices[x1Index]);
            //vect.z = 0.5f;
            //GameObject temp1 = (GameObject)Instantiate(sphere, vect, Quaternion.identity);
           
            float dty = ((t1 - t0) / ((TheMesh.vertices[y1Index].y + (top.transform.position.y + topMod - origTop.y)) -
                (TheMesh.vertices[y0Index].y + (bottom.transform.position.y + bottomMod - origBottom.y))));
            //float dty = ((t1 - t0) / (TheMesh.vertices[y1Index].y - TheMesh.vertices[y0Index].y));
            //vect = transform.TransformPoint(TheMesh.vertices[y0Index]);
            //vect.z = 0.5f;
            //GameObject temp2 = (GameObject)Instantiate(sphere, vect, Quaternion.identity);
            //vect = transform.TransformPoint(TheMesh.vertices[y1Index]);
            //vect.z = 0.5f;
            //GameObject temp3 = (GameObject)Instantiate(sphere, vect, Quaternion.identity);

            float sc = (s0 - ((TheMesh.vertices[x0Index].x - (right.transform.position.x + rightMod - origRight.x)) * dsx));
            //float sc = (s0 - (TheMesh.vertices[x0Index].x * dsx));
            float tc = (t0 - ((TheMesh.vertices[y0Index].y + (bottom.transform.position.y + bottomMod - origBottom.y)) * dty));
            //float tc = (t0 - (TheMesh.vertices[y0Index].y * dty));

            for (int i = 0; i < mainBodyArray.Count; i++)
            {
                Vector2 vals = new Vector2(((mainBodyArray[i].VectorLocal.x * dsx + sc) / ImageWidth), ((mainBodyArray[i].VectorLocal.y * dty + tc) / ImageHeight));
                theUVs[mainBodyArray[i].Index] = vals;
                //TheMesh.uv[vertexArray[i].Index] = vals;
            }

            Vector2 val = new Vector2(((TheMesh.vertices[noseIndex].x * dsx + sc) / ImageWidth), ((TheMesh.vertices[noseIndex].y * dty + tc) / ImageHeight));

            for (int i = 0; i < altBodyArray.Count; i++)
            {
                Vector2 vals = new Vector2(((altBodyArray[i].VectorLocal.x * dsx + sc) / ImageWidth), ((altBodyArray[i].VectorLocal.y * dty + tc) / ImageHeight));
                theUVs[altBodyArray[i].Index] = vals;
            }

            TheMesh.uv = theUVs;
        }
	}

	void SetEyes()
	{
		float s0 = EyeRight.x;
		float s1 = EyeRight.x + EyeRight.width;
		float t0 = EyeRight.y;
		float t1 = EyeRight.y + (EyeRight.height / 2);

		float dsx = ((s1 - s0) / ((EyeMesh.vertices [x1RightEye].x - ((left.transform.position.x - origLeft.x) * 3)) - 
			(EyeMesh.vertices [x0RightEye].x - ((right.transform.position.x - origRight.x) * 3))));
		float dty = ((t1 - t0) / ((EyeMesh.vertices [y1RightEye].y + ((top.transform.position.y - origTop.y) * 5)) - 
			(EyeMesh.vertices [y0RightEye].y + ((bottom.transform.position.y - origBottom.y) * 5))));

		float sc = ( s0 - ((EyeMesh.vertices [x0RightEye].x - ((right.transform.position.x - origRight.x) * 3)) * dsx ));
		float tc = ( t0 - ((EyeMesh.vertices [y0RightEye].y + ((bottom.transform.position.y - origBottom.y) * 5)) * dty ));

		for( int i = 0; i < rightVerts.Count; i++ )
		{
			Vector2 vals = new Vector2 ((( rightVerts[i].VectorLocal.x * dsx + sc ) / ImageWidth), (( rightVerts[i].VectorLocal.y * dty + tc ) / ImageHeight));
			eyesUVs [rightVerts[i].Index] = vals;
			//TheMesh.uv[vertexArray[i].Index] = vals;
		}

		s0 = EyeLeft.x;
		s1 = EyeLeft.x + EyeLeft.width;
		t0 = EyeLeft.y;
		t1 = EyeLeft.y + (EyeLeft.height / 2);

		dsx = ((s1 - s0) / ((EyeMesh.vertices [x1LeftEye].x - ((left.transform.position.x - origLeft.x) * 3)) - 
			(EyeMesh.vertices [x0LeftEye].x - ((right.transform.position.x - origRight.x) * 3))));
		dty = ((t1 - t0) / ((EyeMesh.vertices [y1LeftEye].y + ((top.transform.position.y - origTop.y) * 5)) - 
			(EyeMesh.vertices [y0LeftEye].y + ((bottom.transform.position.y - origBottom.y) * 5))));

		sc = ( s0 - ((EyeMesh.vertices [x0LeftEye].x - ((right.transform.position.x - origRight.x) * 3)) * dsx ));
		tc = ( t0 - ((EyeMesh.vertices [y0LeftEye].y + ((bottom.transform.position.y - origBottom.y) * 5)) * dty ));

		for( int i = 0; i < leftVerts.Count; i++ )
		{
			Vector2 vals = new Vector2 ((( leftVerts[i].VectorLocal.x * dsx + sc ) / ImageWidth), (( leftVerts[i].VectorLocal.y * dty + tc ) / ImageHeight));
			eyesUVs [leftVerts[i].Index] = vals;
			//TheMesh.uv[vertexArray[i].Index] = vals;
		}

		EyeMesh.uv = eyesUVs;
	}

	void SetShirt()
	{
		float s0 = (ImageWidth / 2) - 300;
		float s1 = (ImageWidth / 2) + 300;
		float t0 = (ImageHeight / 2) - 300;
		float t1 = (ImageHeight / 2) + 300;

		//float altered1 = ( (( Face.x + Face.width ) + ( EyeLeft.x + EyeLeft.width ) ) / 2 );
		//float altered2 = (( Face.x + EyeRight.x ) / 2 );
		//float alteredFaceWidth = (altered1 - altered2);
		float dsx = ( ( s1 - s0 ) / ( ( ShirtMesh.vertices [x1Shirt].x + ( ( left.transform.position.x - origLeft.x ) * 6 ) ) -
		            ( ShirtMesh.vertices [x0Shirt].x - ( ( right.transform.position.x - origRight.x ) * 6 ) ) ) );
		float dty = ( ( t1 - t0 ) / ( ( ShirtMesh.vertices [y1Shirt].y + ( ( top.transform.position.y - origTop.y ) * 15 ) ) -
		            ( ShirtMesh.vertices [y0Shirt].y - ( ( bottom.transform.position.y - origBottom.y ) * 15 ) ) ) );

		float sc = ( s0 - ( ( ShirtMesh.vertices [x0Shirt].x - ( ( right.transform.position.x - origRight.x ) * 6 ) ) * dsx ) );
		float tc = ( t0 - ( ( ShirtMesh.vertices [y0Shirt].y - ( ( bottom.transform.position.y - origBottom.y ) * 15 ) ) * dty ) );

		try
		{
			for( int i = 0; i < mainShirtArray.Count; i++ )
			{
				Vector2 vals = new Vector2 (( ( mainShirtArray [i].VectorLocal.x * dsx + sc ) / ImageWidth ), ( ( mainShirtArray [i].VectorLocal.y * dty + tc ) / ImageHeight ));
				shirtUVs [mainShirtArray [i].Index] = vals;
				//TheMesh.uv[vertexArray[i].Index] = vals;
			}
		}
		catch( Exception e )
		{
			string temp = e.Message;
		}

		/*Vector2 val = new Vector2 (( ( ShirtMesh.vertices [noseIndex].x * dsx + sc ) / ImageWidth ), ( ( ShirtMesh.vertices [noseIndex].y * dty + tc ) / ImageHeight ));

		for( int i = 0; i < altBodyArray.Count; i++ )
		{
			theUVs [altBodyArray [i].Index] = val;
		}*/

		ShirtMesh.uv = shirtUVs;
	}

	void SetPants()
	{
		float s0 = (ImageWidth / 2) - 300;
		float s1 = (ImageWidth / 2) + 300;
		float t0 = (ImageHeight / 2) - 600;
		float t1 = (ImageHeight / 2);

		//float altered1 = ( (( Face.x + Face.width ) + ( EyeLeft.x + EyeLeft.width ) ) / 2 );
		//float altered2 = (( Face.x + EyeRight.x ) / 2 );
		//float alteredFaceWidth = (altered1 - altered2);
		float dsx = ( ( s1 - s0 ) / ( ( PantMesh.vertices [x1Pants].x - ( ( left.transform.position.x - origLeft.x ) * 6 ) ) -
			( PantMesh.vertices [x0Pants].x + ( ( right.transform.position.x - origRight.x ) * 6 ) ) ) );
		float dty = ( ( t1 - t0 ) / ( ( PantMesh.vertices [y1Pants].y - ( ( top.transform.position.y - origTop.y ) * 15 ) ) -
			( PantMesh.vertices [y0Pants].y + ( ( bottom.transform.position.y - origBottom.y ) * 15 ) ) ) );

		float sc = ( s0 - ( ( PantMesh.vertices [x0Pants].x + ( ( right.transform.position.x - origRight.x ) * 6 ) ) * dsx ) );
		float tc = ( t0 - ( ( PantMesh.vertices [y0Pants].y + ( ( bottom.transform.position.y - origBottom.y ) * 15 ) ) * dty ) );

		try
		{
			for( int i = 0; i < mainShirtArray.Count; i++ )
			{
				Vector2 vals = new Vector2 (( ( mainPantsArray [i].VectorLocal.x * dsx + sc ) / ImageWidth ), ( ( mainPantsArray [i].VectorLocal.y * dty + tc ) / ImageHeight ));
				pantsUVs [mainPantsArray [i].Index] = vals;
				//TheMesh.uv[vertexArray[i].Index] = vals;
			}
		}
		catch( Exception e )
		{
			string temp = e.Message;
		}

		/*Vector2 val = new Vector2 (( ( ShirtMesh.vertices [noseIndex].x * dsx + sc ) / ImageWidth ), ( ( ShirtMesh.vertices [noseIndex].y * dty + tc ) / ImageHeight ));

		for( int i = 0; i < altBodyArray.Count; i++ )
		{
			theUVs [altBodyArray [i].Index] = val;
		}*/

		PantMesh.uv = pantsUVs;
	}

    void SetUVs2()
    {

    }

	void SetOldUVs()
	{
		TheMesh.uv = theOldUvs;
	}
    #endregion
}
