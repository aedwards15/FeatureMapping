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
    
	#region PUBLIC_INITIALIZERS

	//An object to be instantiated where points are for visual confirmation (temporary object)
	public GameObject pointLocation;

	//The position and sizes for different facial features
	//Currently need to be typed in, future version will be automatically grabbed from FacialDetection app
	public Rect EyeLeft = new Rect();
	public Rect EyeRight = new Rect ();
	public Rect Nose = new Rect ();
	public Rect Mouth = new Rect();
	public Rect Face = new Rect();

	//The size of the photo being used
	public int ImageWidth = 0;
	public int ImageHeight = 0;

	//Whether or not photo is full body or face
	public bool isFullBody = false;

	#endregion

	#region PRIVATE_INTIALIZERS

	private VideoPicker videoPicker;

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

    private class AdjustmentList
    {
        public AdjustmentList ReferencedAdjustments = null;

        public float LeftModification = 0f;
        public float RightModification = 0f;
        public float TopModification = 0f;
        public float BottomModification = 0f;

        public AdjustmentList()
        {
            LeftModification = 0f;
            RightModification = 0f;
            TopModification = 0f;
            BottomModification = 0f;
        }

        public AdjustmentList(AdjustmentList myList)
        {
            ReferencedAdjustments = myList;
        }
    }

	enum state
	{
		Body,
		Face,
        EyeR,
		EyeL,
		Shirt,
		Pants
	}

	//private Mesh theMesh;
	Vector2[] theOldUvs;
	Vector2[] theUVs;
	Vector2[] eyesUVs;
	Vector2[] shirtUVs;
	Vector2[] pantsUVs;

	List<VectorI> mainBodyArray;
	List<VectorI> altBodyArray;
	List<VectorI> rightEyeArray;
	List<VectorI> leftEyeArray;
	List<VectorI> mainShirtArray;
	List<VectorI> altShirtArray;
	List<VectorI> mainPantsArray;
	List<VectorI> altPantsArray;

    state currentState = state.Body;

	Mesh TheMesh;
	Mesh EyeMesh;
	Mesh ShirtMesh;
	Mesh PantMesh;

	//Index of nose (ie middle of face)
    int noseIndex = -1;
	//Index of far right point (from model's perspective) for mapping
	int x0Index = -1;
	//Index of far left point (from model's perspective) for mapping
	int x1Index = -1;
	//Index of bottom point for mapping
	int y0Index = -1;
	//Index of top point for mapping
	int y1Index = -1;

	//original locations of adjusters
	Vector3 origRight = new Vector3();
	Vector3 origLeft = new Vector3();
	Vector3 origTop = new Vector3();
	Vector3 origBottom = new Vector3();

    //how far adjusters have moved
    AdjustmentList currentMods;
    AdjustmentList faceMods = new AdjustmentList();
    AdjustmentList shirtMods = new AdjustmentList();
    AdjustmentList pantMods = new AdjustmentList();
    AdjustmentList eyeLMods = new AdjustmentList();
    AdjustmentList eyeRMods = new AdjustmentList();

	//the adjusters themselves (unitialized)
    GameObject left;
	GameObject right;
	GameObject top;
	GameObject bottom;

	private GameObject gameCamera;
	private GameObject playerModel;
	#endregion
	#endregion

	#region START

	StreamWriter errorWriter;
	void Start()
	{
		errorWriter = new StreamWriter( Application.persistentDataPath + "/Errors.txt", true);
		errorWriter.WriteLine( "Start\n");
		errorWriter.Close();

		//Each part of the model is labeled, it's either the Eyes, the Shirt, the Pants, or everything else
		//The file rests on the face and body mesh, and then finds the rest via their tags
		TheMesh = this.gameObject.GetComponent<MeshFilter>().mesh;
		EyeMesh = GameObject.FindGameObjectWithTag( "Eyes" ).GetComponent<MeshFilter>().mesh;
		ShirtMesh = GameObject.FindGameObjectWithTag( "Shirt" ).GetComponent<MeshFilter>().mesh;
		PantMesh = GameObject.FindGameObjectWithTag( "Pants" ).GetComponent<MeshFilter>().mesh;

		//Grab the main camera
        gameCamera = GameObject.FindGameObjectWithTag("MainCamera");
		//Grab the whole model ("this" is only the face and hands and feet)
        playerModel = GameObject.FindGameObjectWithTag("Player");

		//Move the player into fullview of the camera (hardcoded, maybe need a better way to figure this out...)
        playerModel.transform.position = new Vector3(0.0f, -2.19f, -4.82f);

		//Create the arrays and fill the UV values from the meshs
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

		//This is an array of the "important" vertices (ie the face)
		mainBodyArray = new List<VectorI> ();

		videoPicker = GetComponent<VideoPicker>();

		if (File.Exists( "/private/var/mobile/Containers/Shared/AppGroup/92805BB8-0BDB-496D-A014-68589223B854/Data.txt"))
		{
			errorWriter = new StreamWriter( Application.persistentDataPath + "/Errors.txt", true);
			errorWriter.WriteLine( "Made it to data.txt parse" );
			errorWriter.Close();
			var sr = File.OpenText( "/private/var/mobile/Containers/Shared/AppGroup/92805BB8-0BDB-496D-A014-68589223B854/Data.txt" );
			String data = sr.ReadToEnd();
			sr.Close();
			string[] vals = data.Split(new string[]{"\n"}, StringSplitOptions.RemoveEmptyEntries );

			videoPicker.Path = vals[ 0 ];

			string[] rects = vals[ 1 ].Split( new string[]{ "," }, StringSplitOptions.None );
			Face.x = float.Parse(rects[ 0 ]);
			Face.y = float.Parse(rects[ 1 ]);
			Face.width = float.Parse( rects[ 2 ] );
			Face.height = float.Parse(rects[ 3 ]);

			rects = vals[ 2 ].Split( new string[]{ "," }, StringSplitOptions.None );
			EyeLeft.x = float.Parse(rects[ 0 ]);
			EyeLeft.y = float.Parse(rects[ 1 ]);
			EyeLeft.width = float.Parse(rects[ 2 ]);
			EyeLeft.height = float.Parse(rects[ 3 ]);

			rects = vals[ 3 ].Split( new string[]{ "," }, StringSplitOptions.None );
			EyeRight.x = float.Parse( rects[ 0 ] );
			EyeRight.y = float.Parse(rects[ 1 ]);
			EyeRight.width = float.Parse(rects[ 2 ]);
			EyeRight.height = float.Parse(rects[ 3 ]);

			rects = vals[ 4 ].Split( new string[]{ "," }, StringSplitOptions.None );
			Mouth.x = float.Parse(rects[ 0 ]);
			Mouth.y = float.Parse(rects[ 1 ]);
			Mouth.width = float.Parse(rects[ 2 ]);
			Mouth.height = float.Parse(rects[ 3 ]);

			rects = vals[ 5 ].Split( new string[]{ "," }, StringSplitOptions.None );
			Nose.x = float.Parse(rects[ 0 ]);
			Nose.y = float.Parse(rects[ 1 ]);
			Nose.width = float.Parse(rects[ 2 ]);
			Nose.height = float.Parse(rects[ 3 ]);
		}

		if( File.Exists( Application.persistentDataPath + "/HeadVertices.txt" ) )
		{
			File.Delete( Application.persistentDataPath + "/HeadVertices.txt" );
		}

		//On load-up, if the file containing a listing of all 'important' head vertices exists
		if( File.Exists( Application.persistentDataPath + "/HeadVertices.txt" ) )
		{
			//Open up the file
			var sr = File.OpenText( Application.persistentDataPath + "/HeadVertices.txt" );
			String headVerticesText = sr.ReadToEnd();
			sr.Close();

			//And parse the values
			String[] parse = headVerticesText.Split( new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries );

			//Get point associated with nose (ie the highest z-valued item in the head
			noseIndex = Convert.ToInt32( parse[0].Split( '=' ) [1] );
            Instantiate(pointLocation, transform.TransformPoint(TheMesh.vertices[noseIndex]), Quaternion.identity);

			//Get point associated with far right (from model's perspective) of face
            x0Index = Convert.ToInt32( parse[1].Split( '=' ) [1] );
            Instantiate(pointLocation, transform.TransformPoint(TheMesh.vertices[x0Index]), Quaternion.identity);

			//Get point associated with far left (from model's perspective) of face
			x1Index = Convert.ToInt32( parse[2].Split( '=' ) [1] );
            Instantiate(pointLocation, transform.TransformPoint(TheMesh.vertices[x1Index]), Quaternion.identity);

			//Get point associated with far bottom of face
            y0Index = Convert.ToInt32( parse[3].Split( '=' ) [1] );
            Instantiate(pointLocation, transform.TransformPoint(TheMesh.vertices[y0Index]), Quaternion.identity);

			//Get point associated with far top of face
            y1Index = Convert.ToInt32( parse[4].Split( '=' ) [1] );
            Instantiate(pointLocation, transform.TransformPoint(TheMesh.vertices[y1Index]), Quaternion.identity);

			//Get the remainder of the "important" points, their index and their local vertex (starting after the above reference points)
            for ( int i = 5; i < parse.Length; i++ )
			{
				int index = Convert.ToInt32( parse [i].Split( ',' ) [0] );
				mainBodyArray.Add( new VectorI (index, TheMesh.vertices [index]) );
			}

			//Now get the points of the body
			sr = File.OpenText( Application.persistentDataPath + "/BodyVertices.txt" );
			String bodyVerticesText = sr.ReadToEnd();
			sr.Close();

			parse = bodyVerticesText.Split( new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries );

            altBodyArray = new List<VectorI>();

            for ( int i = 0; i < parse.Length; i++ )
			{
				int index = Convert.ToInt32( parse [i].Split( ',' ) [0] );
				altBodyArray.Add( new VectorI (index, transform.TransformPoint( TheMesh.vertices [index] ), TheMesh.vertices [index]) );
			}

			sr = File.OpenText( Application.persistentDataPath + "/EyeVertices.txt" );
			String eyeVerticesText = sr.ReadToEnd();
			parse = eyeVerticesText.Split( new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries );
			x0RightEye = Convert.ToInt32( parse[ 0 ].Split( '=' )[ 1 ] );
			x1RightEye = Convert.ToInt32(parse[ 1 ].Split( '=' )[ 1 ]);
			y1RightEye = Convert.ToInt32(parse[ 2 ].Split( '=' )[ 1 ]);
			y0RightEye = Convert.ToInt32(parse[ 3 ].Split( '=' )[ 1 ]);
			x0LeftEye = Convert.ToInt32(parse[ 4 ].Split( '=' )[ 1 ]);
			x1LeftEye = Convert.ToInt32(parse[ 5 ].Split( '=' )[ 1 ]);
			y1LeftEye = Convert.ToInt32(parse[ 6 ].Split( '=' )[ 1 ]);
			y0LeftEye = Convert.ToInt32(parse[ 7 ].Split( '=' )[ 1 ]);

			//Temporary
			FindShirtValues();
			FindPantsValues();

			//Instantiate(pointLocation, transform.TransformPoint(ShirtMesh.vertices[y0Shirt]), Quaternion.identity);

			//Shirt, and Pants not currently implemented

			/*TextAsset shirtVerticesText = (TextAsset)Resources.Load( "ShirtVertices" );

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
			}*/
		}
		else
		{
			//Otherwise find the values, save them, and load them
			FindValues();
		}

		//Get the adjusters based on their tag
		right = GameObject.FindGameObjectWithTag("Right");
		left = GameObject.FindGameObjectWithTag("Left");
		bottom = GameObject.FindGameObjectWithTag("Bottom");
		top = GameObject.FindGameObjectWithTag("Top");

		//Get their original position
		origRight = right.transform.position;
		origLeft = left.transform.position;
		origBottom = bottom.transform.position;
		origTop = top.transform.position;

		//And hide them
        right.SetActive(false);
        left.SetActive(false);
        bottom.SetActive(false);
        top.SetActive(false);

        currentMods = new AdjustmentList(new AdjustmentList());
        for (int i = 0; i < 6; i++)
        {
            currentState = (state)i;
            SetTheUVs();
        }

    }

	/// <summary>
	/// Finds and saves the vertices associated with the model
	/// </summary>
	void FindValues()
	{
		FindHeadValues();

		FindEyeValues();

		FindShirtValues();

		FindPantsValues();
	}

	/// <summary>
	/// Finds the Values associated with the head and body
	/// Splits into two main categories, the face, and not the face
	/// 
	/// ...Kind of a mess...
	/// </summary>
	void FindHeadValues()
	{
		//Get all vertices associated with main mesh
		for( int i = 0; i < TheMesh.vertices.Length; i++ )
		{
			mainBodyArray.Add( new VectorI (i, TheMesh.vertices [i]) );
		}

		//Initialize a temporary vector to the same size
		VectorI[] temp1 = new VectorI[TheMesh.vertices.Length];

		//Sort the array by y-values
		mainBodyArray = ( from vert in mainBodyArray
			select vert ).OrderBy( y => y.VectorLocal.y ).ToList();

		//We only want the top third-ish of values of the torso (ie the head)
		//So copy the rest of the values to temp
		mainBodyArray.CopyTo( 0, temp1, 0, (int)( mainBodyArray.Count / 1.6f ));

		//and get rid of them from the main array
		mainBodyArray.RemoveRange( 0, (int)( mainBodyArray.Count / 1.6f ) );

		//Next, order by z-value
		mainBodyArray = ( from vert in mainBodyArray
			select vert ).OrderBy( y => y.VectorLocal.z ).ToList();

		//We only want the front third-ish of values of the head (ie the face)
		//So copy the rest of the values to temp
		mainBodyArray.CopyTo( 0, temp1, (int)(TheMesh.vertices.Length / 1.6f ), (int)( mainBodyArray.Count / 2f ) );

		//and get rid of them from the main array
		mainBodyArray.RemoveRange( 0, (int)( mainBodyArray.Count / 2f ) );

        //May not need to instantiate a VectorI for this, but..
        //Grab the lowest x-value point (ie the x-ordering on the model is reverse, so highest is on right from model's perspective)
        //This will be used later for UV mapping
        x0Index = (from vert in mainBodyArray
                   select vert).OrderByDescending(y => y.VectorLocal.x).Take(1).ToList()[0].Index;

        //Now grab lowest x-value point (ie the point on the left side of the face from model's perspective)
        x1Index = ( from vert in mainBodyArray
		            select vert ).OrderBy( y => y.VectorLocal.x ).Take( 1 ).ToList()[ 0 ].Index;

		//Grab the lowest y-value point (ie the point lowest on the face)
		y0Index = ( from vert in mainBodyArray
		            select vert ).OrderBy( y => y.VectorLocal.y ).Take( 1 ).ToList()[ 0 ].Index;

		//Grab the highest y-value point (ie the point highest on the face)
		y1Index = ( from vert in mainBodyArray
		            select vert ).OrderByDescending( y => y.VectorLocal.y ).Take( 1 ).ToList()[ 0 ].Index;

		//Grab the highest z-value point (ie the nose)
		noseIndex = ( from vert in mainBodyArray
		              select vert ).OrderByDescending( y => y.VectorLocal.z ).Take( 1 ).ToList()[ 0 ].Index;

		//Fill the alternate body array with discarded points
		altBodyArray = new List<VectorI> ();
		altBodyArray = ( from vert in temp1
			where vert != null
			select vert).ToList();

		//If the file exists (which it shouldn't since that's what got us here) delete it
		if( File.Exists( Application.persistentDataPath + "/HeadVertices.txt" ) )
		{
			File.Delete( Application.persistentDataPath + "/HeadVertices.txt" );
		}

		//Create the file, specifying notworthy points first
		var sr = File.CreateText( Application.persistentDataPath + "/HeadVertices.txt" );
		sr.WriteLine( Application.persistentDataPath + "/HeadVertices.txt" );
		sr.WriteLine( String.Format( "nose={0}", noseIndex ) );
		sr.WriteLine( String.Format( "right={0}", x0Index ) );
		sr.WriteLine( String.Format( "left={0}", x1Index ) );
		sr.WriteLine( String.Format( "bottom={0}", y0Index ) );
		sr.WriteLine( String.Format( "top={0}", y1Index ) );

		//Then writing in the rest of the 'face' values
		foreach( var item in mainBodyArray )
		{
			sr.WriteLine( String.Format( "{0},{1},{2},{3}", item.Index, item.VectorLocal.x, item.VectorLocal.y, item.VectorLocal.z ) );
		}
		sr.Close();

		//Then write in the rest of the body's points in their own file
		//May get rid of this if I get time to try a different way of filling in other values
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

		rightVerts.RemoveRange( 0, (int)(rightVerts.Count / 2.5f) );

		leftVerts.RemoveRange( 0, (int)(leftVerts.Count / 2.5f) );

		VectorI temp = ( from vert in rightVerts
		                 select vert ).OrderByDescending( y => y.VectorLocal.x ).Take( 1 ).ToList() [0];
		x0RightEye = temp.Index;
        //Instantiate(pointLocation, transform.TransformPoint(EyeMesh.vertices[x0RightEye]), Quaternion.identity);

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

		for( int i = 0; i < ShirtMesh.vertices.Length; i++ )
		{
			mainShirtArray.Add( new VectorI (i, ShirtMesh.vertices [i]) );
		}

        //Initialize a temporary vector to the same size
        VectorI[] temp1 = new VectorI[ShirtMesh.vertices.Length];
        
        mainShirtArray = (from vert in mainShirtArray
                          select vert).OrderBy(y => y.VectorLocal.x).ToList();

        int maths = (int)(mainShirtArray.Count * (1f / 4f));
        mainShirtArray.CopyTo(0, temp1, 0, maths);
        
        mainShirtArray.RemoveRange(0, maths);
        
        mainShirtArray = (from vert in mainShirtArray
                          select vert).OrderByDescending(y => y.VectorLocal.x).ToList();
        
        mainShirtArray.CopyTo(0, temp1, maths, maths);
        
        mainShirtArray.RemoveRange(0, maths);

        mainShirtArray = ( from vert in mainShirtArray
			select vert ).OrderBy( y => y.VectorLocal.z ).ToList();

        maths = (mainShirtArray.Count / 2);
        mainShirtArray.CopyTo(0, temp1, (maths*2), maths);

        mainShirtArray.RemoveRange( 0, maths);

        altShirtArray = (from vert in temp1
                        where vert != null
                        select vert).ToList();

        mainShirtArray = (from vert in mainShirtArray
                          select vert).OrderBy(y => y.VectorLocal.x).ToList();

        VectorI temp = ( from vert in mainShirtArray
		                 select vert ).OrderByDescending( y => y.VectorLocal.x ).Take( 1 ).ToList()[ 0 ];
		x0Shirt = temp.Index;

        temp = ( from vert in mainShirtArray
		         select vert ).OrderBy( y => y.VectorLocal.x ).Take( 1 ).ToList()[ 0 ];
		x1Shirt = temp.Index;

		temp = ( from vert in mainShirtArray
		         select vert ).OrderBy( y => y.VectorLocal.y ).Take( 1 ).ToList()[ 0 ];
		y0Shirt = temp.Index;

        temp = ( from vert in mainShirtArray
		         select vert ).OrderByDescending( y => y.VectorLocal.y ).Take( 1 ).ToList()[ 0 ];
		y1Shirt = temp.Index;
    }

	int x0Pants;
	int x1Pants;
	int y0Pants;
	int y1Pants;
	void FindPantsValues()
	{
		mainPantsArray = new List<VectorI> ();
		//altPantsArray = new List<VectorI> ();
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
		                 select vert ).OrderByDescending( y => y.VectorLocal.x ).Take( 1 ).ToList()[ 0 ];
		x0Pants = temp.Index;

		temp = ( from vert in mainPantsArray
		         select vert ).OrderBy( y => y.VectorLocal.x ).Take( 1 ).ToList()[ 0 ];
		x1Pants = temp.Index;

		temp = ( from vert in mainPantsArray
			select vert ).OrderBy( y => y.VectorLocal.y ).Take( 1 ).ToList() [0];
		y0Pants = temp.Index;

		temp = ( from vert in mainPantsArray
			select vert ).OrderByDescending( y => y.VectorLocal.y ).Take( 1 ).ToList() [0];
		y1Pants = temp.Index;
	}
    #endregion

    #region UPDATE

	//Used in Update, seemed silly to recreate these each time Update is called (every 1/60th a second)
	private Vector3 screenPoint;
	private Vector3 offset;

	private GameObject firstPoint;
	private GameObject secondPoint;

	private GameObject selectedPoint;

    private bool isRightEye = false;
	private bool test = true;
    void Update()
	{
		if( test)
		{
			errorWriter = new StreamWriter( Application.persistentDataPath + "/Errors.txt", true);
			errorWriter.WriteLine( "Inside videopicker update" );
			errorWriter.Close();

			if (File.Exists("/private/var/mobile/Containers/Shared/AppGroup/92805BB8-0BDB-496D-A014-68589223B854/myPhoto.jpg")) {
				errorWriter = new StreamWriter( Application.persistentDataPath + "/Errors.txt", true);
				errorWriter.WriteLine( "Inside if exists update" );
				errorWriter.Close();

				Texture2D tex = null;
				byte[] fileData;

				try
				{
					fileData = File.ReadAllBytes("/private/var/mobile/Containers/Shared/AppGroup/92805BB8-0BDB-496D-A014-68589223B854/myPhoto.jpg");
					tex = new Texture2D(2, 2);
					tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.

					this.gameObject.GetComponent<Material>().mainTexture = tex;
					GameObject gObj = GameObject.Find( "shirt01.obj" );
					gObj.GetComponent<Material>().mainTexture = tex;
					gObj = GameObject.Find( "jeans01.obj" );
					gObj.GetComponent<Material>().mainTexture = tex;
					gObj = GameObject.Find( "high-poly.obj" );
					gObj.GetComponent<Material>().mainTexture = tex;


					errorWriter = new StreamWriter( Application.persistentDataPath + "/Errors.txt", true);
					errorWriter.WriteLine( "End of if exists update" );
					errorWriter.Close();

					test = false;
				}
				catch (Exception e)
				{
					errorWriter = new StreamWriter( Application.persistentDataPath + "/Errors.txt", true);
					errorWriter.WriteLine( "EXCEPTION: " + e.Message);
					errorWriter.Close();
				}
			}
		}

		//Mouse button click handlers
		{ //Blank brace is hear to match up with touch inputs
			//If mouse button clicked down
			if( Input.GetMouseButtonDown( 0 ) )
			{
				//Grab where it hit in the world
				Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );
				RaycastHit hit;

				//If it hit anything
				if( Physics.Raycast( ray, out hit ) )
				{
					//Find out if it hit the player, and where, or if it hit an adjuster
					if( hit.transform.gameObject.name == "Adjuster")
					{
                        selectedPoint = hit.transform.gameObject;

                        screenPoint = Camera.main.WorldToScreenPoint(selectedPoint.transform.position);
                        offset = selectedPoint.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
                    }
                    else if (hit.transform.gameObject.tag == "Head")
                    {
						//If the player tapped on the head, zoom in to the face and set current state as Face
                        playerModel.transform.position = new Vector3(0.0f, -3.84f, -8.74f);
                        currentState = state.Face;

                        currentMods = new AdjustmentList(faceMods);

                        //Show the adjuster bars
                        left.SetActive(true);
						right.SetActive(true);
						top.SetActive(true);
						bottom.SetActive(true);
                    }
                    else if (hit.transform.gameObject.tag == "Eyes")
                    {
                        if (!isRightEye)
                        {
                            isRightEye = true;
                            playerModel.transform.position = new Vector3(0.06f, -3.84f, -8.99f);
                            currentState = state.EyeR;

                            currentMods = new AdjustmentList(eyeRMods);
                        }
                        else
                        {
                            isRightEye = false;
                            playerModel.transform.position = new Vector3(-0.06f, -3.84f, -8.99f);
                            currentState = state.EyeL;

                            currentMods = new AdjustmentList(eyeLMods);
                        }

                        //Show the adjuster bars
                        left.SetActive(true);
						right.SetActive(true);
						top.SetActive(true);
						bottom.SetActive(true);
                    }
                    else if (hit.transform.gameObject.tag == "Shirt")
                    {
                        playerModel.transform.position = new Vector3(0.0f, -2.944f, -7.549f);
                        currentState = state.Shirt;

                        currentMods = new AdjustmentList(shirtMods);

                        //Show the adjuster bars
                        left.SetActive(true);
						right.SetActive(true);
						top.SetActive(true);
						bottom.SetActive(true);
                    }
                    else if (hit.transform.gameObject.tag == "Pants")
                    {
                        playerModel.transform.position = new Vector3(0.0f, -1.342f, -7.752f);
                        currentState = state.Pants;

                        currentMods = new AdjustmentList(pantMods);

                        //Show the adjuster bars
                        left.SetActive(true);
						right.SetActive(true);
						top.SetActive(true);
						bottom.SetActive(true);
                    }
                }
			}
			else if( Input.GetMouseButton( 0 ) )
			{
				//Else if the mouse buttin is being held down
				if( selectedPoint != null )
				{
					//and an object has been clicked on then
                    Vector3 cursorPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
                    Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(cursorPoint) + offset;

					//If it's and adjuster, move it, and adjust the UVs
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
				//Else if we're releasing the click
				if( selectedPoint != null )
				{
					//and an adjuster was selected, move it back to it's original location and keep track of the difference
                    if (selectedPoint.tag == "Left")
                    {
                        currentMods.ReferencedAdjustments.LeftModification += origLeft.x - selectedPoint.transform.position.x;
                        selectedPoint.transform.position = origLeft;
                    }
                    else if (selectedPoint.tag == "Right")
                    {
                        currentMods.ReferencedAdjustments.RightModification += origRight.x - selectedPoint.transform.position.x;
                        selectedPoint.transform.position = origRight;
                    }
                    else if (selectedPoint.tag == "Top")
                    {
                        currentMods.ReferencedAdjustments.TopModification += origTop.y - selectedPoint.transform.position.y;
                        selectedPoint.transform.position = origTop;
                    }
                    else
                    {
                        currentMods.ReferencedAdjustments.BottomModification += origBottom.y - selectedPoint.transform.position.y;
                        selectedPoint.transform.position = origBottom;
                    }

                    selectedPoint = null;
				}
			}
		}

		//Same thing as mouse clicking above, only grab touches (for iphone)
		if( Input.touchCount > 0)
		{
            if (Input.GetTouch(0).phase == TouchPhase.Began)
            {
                //Grab where it hit in the world
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                //If it hit anything
                if (Physics.Raycast(ray, out hit))
                {
                    //Find out if it hit the player, and where, or if it hit an adjuster
                    if (hit.transform.gameObject.name == "Adjuster")
                    {
                        selectedPoint = hit.transform.gameObject;

                        screenPoint = Camera.main.WorldToScreenPoint(selectedPoint.transform.position);
                        offset = selectedPoint.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
                    }
                    else if (hit.transform.gameObject.tag == "Head")
                    {
                        //If the player tapped on the head, zoom in to the face and set current state as Face
                        playerModel.transform.position = new Vector3(0.0f, -3.84f, -8.74f);
                        currentState = state.Face;

                        currentMods = new AdjustmentList(faceMods);

                        //Show the adjuster bars
                        left.SetActive(true);
                        right.SetActive(true);
                        top.SetActive(true);
                        bottom.SetActive(true);
                    }
                    else if (hit.transform.gameObject.tag == "Eyes")
                    {
                        if (!isRightEye)
                        {
                            isRightEye = true;
                            playerModel.transform.position = new Vector3(0.06f, -3.84f, -8.99f);
                            currentState = state.EyeR;

                            currentMods = new AdjustmentList(eyeRMods);
                        }
                        else
                        {
                            isRightEye = false;
                            playerModel.transform.position = new Vector3(-0.06f, -3.84f, -8.99f);
                            currentState = state.EyeL;

                            currentMods = new AdjustmentList(eyeLMods);
                        }

                        //Show the adjuster bars
                        left.SetActive(true);
                        right.SetActive(true);
                        top.SetActive(true);
                        bottom.SetActive(true);
                    }
                    else if (hit.transform.gameObject.tag == "Shirt")
                    {
                        playerModel.transform.position = new Vector3(0.0f, -2.944f, -7.549f);
                        currentState = state.Shirt;

                        currentMods = new AdjustmentList(shirtMods);

                        //Show the adjuster bars
                        left.SetActive(true);
                        right.SetActive(true);
                        top.SetActive(true);
                        bottom.SetActive(true);
                    }
                    else if (hit.transform.gameObject.tag == "Pants")
                    {
                        playerModel.transform.position = new Vector3(0.0f, -1.342f, -7.752f);
                        currentState = state.Pants;

                        currentMods = new AdjustmentList(pantMods);

                        //Show the adjuster bars
                        left.SetActive(true);
                        right.SetActive(true);
                        top.SetActive(true);
                        bottom.SetActive(true);
                    }
                }
            }
            else if (Input.GetTouch(0).phase == TouchPhase.Moved)
            {
                //Else if the mouse buttin is being held down
                if (selectedPoint != null)
                {
                    //and an object has been clicked on then
                    Vector3 cursorPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
                    Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(cursorPoint) + offset;

                    //If it's and adjuster, move it, and adjust the UVs
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
                //Else if we're releasing the click
                if (selectedPoint != null)
                {
                    //and an adjuster was selected, move it back to it's original location and keep track of the difference
                    if (selectedPoint.tag == "Left")
                    {
                        currentMods.ReferencedAdjustments.LeftModification += origLeft.x - selectedPoint.transform.position.x;
                        selectedPoint.transform.position = origLeft;
                    }
                    else if (selectedPoint.tag == "Right")
                    {
                        currentMods.ReferencedAdjustments.RightModification += origRight.x - selectedPoint.transform.position.x;
                        selectedPoint.transform.position = origRight;
                    }
                    else if (selectedPoint.tag == "Top")
                    {
                        currentMods.ReferencedAdjustments.TopModification += origTop.y - selectedPoint.transform.position.y;
                        selectedPoint.transform.position = origTop;
                    }
                    else
                    {
                        currentMods.ReferencedAdjustments.BottomModification += origBottom.y - selectedPoint.transform.position.y;
                        selectedPoint.transform.position = origBottom;
                    }

                    selectedPoint = null;
                }
            }
		}
	}
    #endregion

    #region EVENT

	void SetTheUVs()
	{
		//Find out which object has been selected and set there UV values
        if (currentState == state.EyeR || currentState == state.EyeL)
            SetEyes();
        else if (currentState == state.Shirt)
            SetShirt();
        else if (currentState == state.Pants)
            SetPants();
        else
        {
            //////////////////////////////
            /// 
            /// Algorithm for UV Mapping from Image to Model
            /// 
            /// S = X * dsx + sc
            /// sc = s0 - x0 * dsx
            /// dsx = (s1 - s0) / (x1 - x0)
            /// 
            /// and
            /// 
            /// T = Y * dty + tc
            /// tc = t0 - y0 * dty
            /// dty = (t1 - t0) / (y1 - y0)
            /// 
            /// Where:
            /// (S,T) are the coordinates on the image calculated from the (X,Y) coordinate of the model
            /// (x0,y0),(x1,y1) and (s0,t0),(s1,t1) describe the bounds of the model and the image respectively
            /// 
            //////////////////////////////

            //Get the points needed for mapping from the supplied image
            /*float s0 = EyeRight.x;
            float s1 = EyeLeft.x + EyeLeft.width;
            float t0 = Mouth.y;
            float t1 = EyeLeft.y + EyeLeft.height;*/

            float s0 = Face.x;
            float s1 = Face.x + Face.width;
            float t0 = Face.y;
            float t1 = Face.y + Face.height;

            Vector2 leftEyeStartingPoint = new Vector2((EyeLeft.x / ImageWidth), (EyeLeft.y / ImageHeight));
            Vector2 leftEyeEndingPoint = new Vector2(((EyeLeft.x + EyeLeft.width) / ImageWidth), ((EyeLeft.y + EyeLeft.height) / ImageHeight));
            Rect leftEyeUVRect = new Rect(leftEyeStartingPoint, leftEyeEndingPoint);

            //calculate values as per algorithm using the points given in image and model
            //adjusting to user input
            float dsx = ((s1 - s0) / ((TheMesh.vertices[x1Index].x - (((origLeft.x - left.transform.position.x) + currentMods.ReferencedAdjustments.LeftModification) * -1)) -
                                 (TheMesh.vertices[x0Index].x - (((origRight.x - right.transform.position.x) + currentMods.ReferencedAdjustments.RightModification) * -1))));
            float dty = ((t1 - t0) / ((TheMesh.vertices[y1Index].y + (((origTop.y - top.transform.position.y) + currentMods.ReferencedAdjustments.TopModification) * -1)) -
                                 (TheMesh.vertices[y0Index].y + (((origBottom.y - bottom.transform.position.y) + currentMods.ReferencedAdjustments.BottomModification) * -1))));

            float sc = (s0 - ((TheMesh.vertices[x0Index].x - (((origRight.x - right.transform.position.x) + currentMods.ReferencedAdjustments.RightModification) * -1)) * dsx));
            float tc = (t0 - ((TheMesh.vertices[y0Index].y + (((origBottom.y - bottom.transform.position.y) + currentMods.ReferencedAdjustments.BottomModification) * -1)) * dty));

            Vector2 val = new Vector2(((TheMesh.vertices[noseIndex].x * dsx + sc) / ImageWidth), ((TheMesh.vertices[noseIndex].y * dty + tc) / ImageHeight));

            Rect altEyeLeft = new Rect(EyeLeft.x, EyeLeft.y, EyeLeft.width, (EyeLeft.height * (1f/2f)));
            Rect altEyeRight = new Rect(EyeRight.x, EyeRight.y, EyeRight.width, (EyeRight.height * (1f/2f)));

            //calculate what UV values should be mapped and map them
            for (int i = 0; i < mainBodyArray.Count; i++)
            {
                Vector2 vals = new Vector2(((mainBodyArray[i].VectorLocal.x * dsx + sc)), ((mainBodyArray[i].VectorLocal.y * dty + tc)));
                if (!altEyeLeft.Contains(vals) && !altEyeRight.Contains(vals) && Face.Contains(vals))
                {
                    vals.x = (vals.x / ImageWidth);
                    vals.y = (vals.y / ImageHeight);
                }
                else
                {
                    vals = val;
                }
                theUVs[mainBodyArray[i].Index] = vals;
            }
            
            for (int i = 0; i < altBodyArray.Count; i++)
            {
                //for all values not in the face, grab a generic point, at the nose, and color the rest of the body accordingly
                //Vector2 vals = new Vector2(((altBodyArray[i].VectorLocal.x * dsx + sc) / ImageWidth), ((altBodyArray[i].VectorLocal.y * dty + tc) / ImageHeight));
                theUVs[altBodyArray[i].Index] = val;
            }

            TheMesh.uv = theUVs;
        }
	}
    
    void SetEyes()
    {
        if (currentState == state.EyeR)
        {
            float s0 = EyeRight.x;
            float s1 = EyeRight.x + EyeRight.width;
            float t0 = EyeRight.y;
            float t1 = EyeRight.y + (EyeRight.height / 1.5f);

            float dsx = ((s1 - s0) / ((EyeMesh.vertices[x1RightEye].x - (((origLeft.x - left.transform.position.x) + currentMods.ReferencedAdjustments.LeftModification) * -1)) -
                (EyeMesh.vertices[x0RightEye].x - (((origRight.x - right.transform.position.x) + currentMods.ReferencedAdjustments.RightModification) * -1))));
            float dty = ((t1 - t0) / ((EyeMesh.vertices[y1RightEye].y + (((origTop.y - top.transform.position.y) + currentMods.ReferencedAdjustments.TopModification) * -1)) -
                (EyeMesh.vertices[y0RightEye].y + (((origBottom.y - bottom.transform.position.y) + currentMods.ReferencedAdjustments.BottomModification) * -1))));

            float sc = (s0 - ((EyeMesh.vertices[x0RightEye].x - (((origRight.x - right.transform.position.x) + currentMods.ReferencedAdjustments.RightModification) * -1)) * dsx));
            float tc = (t0 - ((EyeMesh.vertices[y0RightEye].y + (((origBottom.y - bottom.transform.position.y) + currentMods.ReferencedAdjustments.BottomModification) * -1)) * dty));

            for (int i = 0; i < rightVerts.Count; i++)
            {
                Vector2 vals = new Vector2(((rightVerts[i].VectorLocal.x * dsx + sc) / ImageWidth), ((rightVerts[i].VectorLocal.y * dty + tc) / ImageHeight));
                eyesUVs[rightVerts[i].Index] = vals;
                //TheMesh.uv[vertexArray[i].Index] = vals;
            }
        }

        if (currentState == state.EyeL)
        {
            float s0 = EyeLeft.x;
            float s1 = EyeLeft.x + EyeLeft.width;
            float t0 = EyeLeft.y;
            float t1 = EyeLeft.y + (EyeLeft.height / 1.5f);

            float dsx = ((s1 - s0) / ((EyeMesh.vertices[x1LeftEye].x - (((origLeft.x - left.transform.position.x) + currentMods.ReferencedAdjustments.LeftModification) * -1)) -
                (EyeMesh.vertices[x0LeftEye].x - (((origRight.x - right.transform.position.x) + currentMods.ReferencedAdjustments.RightModification) * -1))));
            float dty = ((t1 - t0) / ((EyeMesh.vertices[y1LeftEye].y + (((origTop.y - top.transform.position.y) + currentMods.ReferencedAdjustments.TopModification) * -1)) -
                (EyeMesh.vertices[y0LeftEye].y + (((origBottom.y - bottom.transform.position.y) + currentMods.ReferencedAdjustments.BottomModification) * -1))));

            float sc = (s0 - ((EyeMesh.vertices[x0LeftEye].x - (((origRight.x - right.transform.position.x) + currentMods.ReferencedAdjustments.RightModification) * -1)) * dsx));
            float tc = (t0 - ((EyeMesh.vertices[y0LeftEye].y + (((origBottom.y - bottom.transform.position.y) + currentMods.ReferencedAdjustments.BottomModification) * -1)) * dty));

            for (int i = 0; i < leftVerts.Count; i++)
            {
                Vector2 vals = new Vector2(((leftVerts[i].VectorLocal.x * dsx + sc) / ImageWidth), ((leftVerts[i].VectorLocal.y * dty + tc) / ImageHeight));
                eyesUVs[leftVerts[i].Index] = vals;
                //TheMesh.uv[vertexArray[i].Index] = vals;
            }
        }

        EyeMesh.uv = eyesUVs;
    }

    void SetShirt()
    {
        float s0 = (ImageWidth / 2) - 400;
        float s1 = (ImageWidth / 2) + 400;
        float t0 = (ImageHeight / 2) - 100;
        float t1 = (ImageHeight / 2) + 1000;

        float dsx = ((s1 - s0) / ((ShirtMesh.vertices[x1Shirt].x + (((origLeft.x - left.transform.position.x) + currentMods.ReferencedAdjustments.LeftModification) * 5)) -
                    (ShirtMesh.vertices[x0Shirt].x - (((origRight.x - right.transform.position.x) + currentMods.ReferencedAdjustments.RightModification) * -5))));
        float dty = ((t1 - t0) / ((ShirtMesh.vertices[y1Shirt].y + (((origTop.y - top.transform.position.y) + currentMods.ReferencedAdjustments.TopModification) * -5)) -
                    (ShirtMesh.vertices[y0Shirt].y - (((origBottom.y - bottom.transform.position.y) + currentMods.ReferencedAdjustments.BottomModification) * 5))));

        float sc = (s0 - ((ShirtMesh.vertices[x0Shirt].x - (((origRight.x - right.transform.position.x) + currentMods.ReferencedAdjustments.RightModification) * -5)) * dsx));
        float tc = (t0 - ((ShirtMesh.vertices[y0Shirt].y - (((origBottom.y - bottom.transform.position.y) + currentMods.ReferencedAdjustments.BottomModification) * 5)) * dty));
        
        for (int i = 0; i < mainShirtArray.Count; i++)
        {
            Vector2 vals = new Vector2(((mainShirtArray[i].VectorLocal.x * dsx + sc) / ImageWidth), ((mainShirtArray[i].VectorLocal.y * dty + tc) / ImageHeight));
            shirtUVs[mainShirtArray[i].Index] = vals;
        }

        Vector2 val = new Vector2 (( ( ShirtMesh.vertices [y0Shirt].x * dsx + sc ) / ImageWidth ), ( ( ShirtMesh.vertices [y0Shirt].y * dty + tc ) / ImageHeight ));

		for( int i = 0; i < altShirtArray.Count; i++ )
		{
            shirtUVs[altShirtArray[i].Index] = val;
		}

        ShirtMesh.uv = shirtUVs;
    }

    void SetPants()
	{
		float s0 = (ImageWidth / 2) - 300;
		float s1 = (ImageWidth / 2) + 300;
		float t0 = (ImageHeight / 2) - 800;
		float t1 = (ImageHeight / 2) - 100;

        float dsx = ((s1 - s0) / ((PantMesh.vertices[x1Pants].x + (((origLeft.x - left.transform.position.x) + currentMods.ReferencedAdjustments.LeftModification) * 5)) -
                    (PantMesh.vertices[x0Pants].x - (((origRight.x - right.transform.position.x) + currentMods.ReferencedAdjustments.RightModification) * -5))));
        float dty = ((t1 - t0) / ((PantMesh.vertices[y1Pants].y + (((origTop.y - top.transform.position.y) + currentMods.ReferencedAdjustments.TopModification) * -5)) -
                    (PantMesh.vertices[y0Pants].y - (((origBottom.y - bottom.transform.position.y) + currentMods.ReferencedAdjustments.BottomModification) * 5))));

        float sc = (s0 - ((PantMesh.vertices[x0Pants].x - (((origRight.x - right.transform.position.x) + currentMods.ReferencedAdjustments.RightModification) * -5)) * dsx));
        float tc = (t0 - ((PantMesh.vertices[y0Pants].y - (((origBottom.y - bottom.transform.position.y) + currentMods.ReferencedAdjustments.BottomModification) * 5)) * dty));
        
		for( int i = 0; i < mainPantsArray.Count; i++ )
		{
			Vector2 vals = new Vector2 (( ( mainPantsArray [i].VectorLocal.x * dsx + sc ) / ImageWidth ), ( ( mainPantsArray [i].VectorLocal.y * dty + tc ) / ImageHeight ));
			pantsUVs [mainPantsArray [i].Index] = vals;
			//TheMesh.uv[vertexArray[i].Index] = vals;
		}

		/*Vector2 val = new Vector2 (( ( ShirtMesh.vertices [noseIndex].x * dsx + sc ) / ImageWidth ), ( ( ShirtMesh.vertices [noseIndex].y * dty + tc ) / ImageHeight ));

		for( int i = 0; i < altBodyArray.Count; i++ )
		{
			theUVs [altBodyArray [i].Index] = val;
		}*/

		PantMesh.uv = pantsUVs;
	}
    #endregion
}
