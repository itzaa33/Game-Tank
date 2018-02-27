//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using System;
//
//using System.IO;
//using System.Runtime.Serialization.Formatters.Binary;
//
//public static class BinaryUtil : MonoBehaviour 
//{
//
//    public static void Serialize(string filePath,object target)
//    {
//
//        string path = Path.Combine(Application.persistentDataPath, filePath);
//
//        FileStream stream = File.Open(path, FileMode.OpenOrCreate);
//        BinaryFormatter bf = new BinaryFormatter();
//
//        bf.Serialize(stream, target);
//
//        stream.Close();
//    }
//
//    public static T Deserialize<T>(string filePath, T defaultValue = default(T))
//    {
//        string path = Path.Combine(Application.persistentDataPath, filePath);
//
//        if(File.Exists(path))
//        {
//            FileStream stream = File.Open(path, FileMode.Open);
//            BinaryFormatter bf = new BinaryFormatter();
//
//            T result = (T)bf.Deserialize(stream);
//
//            stream.Close();
//            return result;
//        }
//
//        return defaultValue;
//    }
//
//    public static void Delete(string filePath)
//    {
//        string path = Path.Combine(Application.persistentDataPath, filePath);
//
//        if (File.Exists(path))
//        {
//            File.Delete(path);
//        }
//    }
//	
//}
