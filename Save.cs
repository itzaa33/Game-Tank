using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;


public class Save : MonoBehaviour 
{

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            string path = Application.persistentDataPath + "/save.text";

//                Serialize(path,);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            string path = Application.persistentDataPath + "/save.text";

//            Serialize(path,);
        }


    }

    void Serialize(string filePath,object target)
    {
        FileStream stream = File.Open(filePath, FileMode.OpenOrCreate);

        BinaryFormatter bf = new BinaryFormatter();

        bf.Serialize(stream, target);

        stream.Close();
    }

//     void T Deserialize<T>(string filePath)
//    {
//        if(File.Exists(filePath))
//        {
//            FileStream stream = File.Open(filePath, FileMode.Open);
//            BinaryFormatter bf = new BinaryFormatter();
//
//            T result = (T)bf.Deserialize(stream);
//
//            stream.Close();
//            return result;
//        }
//
//        return default(T);
//    }

}
