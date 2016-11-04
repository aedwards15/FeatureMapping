using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;

public class VideoPicker : MonoBehaviour {

	public Texture2D shareButtonImage; // Use this for initialization

	[DllImport("__Internal")]
	private static extern void OpenVideoPicker(string game_object_name, string function_name);
	
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public bool isDone = false;
	void OnGUI ()
	{
		//if( !isDone )
		{
			//this.transform.position = new Vector3( 0, 0, -7 );
			if( GUILayout.Button( shareButtonImage, GUIStyle.none, GUILayout.Width( 128 ), GUILayout.Height( 128 ) ) )
			{
				//this.transform.position = new Vector3( 0, 0, -6 );
				OpenVideoPicker( "base.obj", "VideoPicked" );
			}
		}
	}

	public string Path = "";

	void VideoPicked( string path ){
		Debug.Log ("---->VideoPicked");
		Debug.Log( path );
		Path = path;

		if( File.Exists( Application.persistentDataPath + "/path.txt" ) )
		{
			File.Delete( Application.persistentDataPath + "/path.txt" );
		}

		//Create the file, specifying notworthy points first
		var sr = File.CreateText( Application.persistentDataPath + "/path.txt" );
		sr.WriteLine( path );
		sr.Close();
	}
}
