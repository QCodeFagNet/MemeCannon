using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MemeCannon
{
	public class FileHelper
	{
		private static Object _locker = new Object();
		public FileHelper() { }

		/// <summary>Helper method to read JSON data from a local file</summary>
		public static JArray ReadJSONFromFile(string dataFilePath)
		{
			string fileString;
			if (File.Exists(dataFilePath))
			{
				//while (!IsFileLocked(dataFilePath)) { }
				using (FileStream stream = File.Open(dataFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					using (StreamReader reader = new StreamReader(stream))
					{
						fileString = reader.ReadToEnd();
						reader.Close();
					}
					stream.Close();
				}
				return JArray.Parse(fileString);
			}
			else return null;
		}

		public static string ReadTextFromFile(string dataFilePath)
		{
			Task<string> task = _ReadJSONFromFileAsync(dataFilePath);
			task.Wait();
			return task.Result;
		}

		/// <summary>Helper method to read JSON data from a local file</summary>
		public static JObject ReadJSONObjectFromFile(string dataFilePath)
		{
			return JObject.Parse(ReadTextFromFile(dataFilePath));
		}

		private static async Task<string> _ReadJSONFromFileAsync(string filePath)
		{
			lock (_locker)
			{
				string fileString;
				if (File.Exists(filePath))
				{
					using (FileStream sourceStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					{
						using (StreamReader reader = new StreamReader(sourceStream))
						{
							fileString = reader.ReadToEnd();
						}
						sourceStream.Close();
						return fileString;
					}
				}
				return null;
			}
		}

		public static async Task<string> _ReadStringFromFileAsync(string filePath)
		{
			string fileString;
			if (File.Exists(filePath))
			{
				using (FileStream sourceStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					using (StreamReader reader = new StreamReader(sourceStream))
					{
						fileString = reader.ReadToEnd();
					}
					sourceStream.Close();
					return fileString;
				}
			}
			return null;
		}

		/// <summary>Helper method to write JSON data to a local file</summary>
		static async Task WriteJSONToFileAsync(string filePath, string jsonData)
		{
			//https://stackoverflow.com/questions/8630736/getting-an-outofmemoryexception-while-serialising-to-json
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(filePath));
				//File.WriteAllText(@filePath, jsonData);
				byte[] encodedText = Encoding.UTF8.GetBytes(jsonData);
				//while (!IsFileLocked(filePath)) { }
				using (FileStream sourceStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, bufferSize: 4096, useAsync: true))
				{
					await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
					sourceStream.Close();
				}
			}
			catch (Exception ex)
			{
				// Eatit
			}
		}
		/// <summary>Helper method to write JSON data to a local file</summary>
		public static void WriteJSONToFile(string filePath, string jsonData)
		{
			//File.WriteAllText(@filePath, jsonData);
			WriteJSONToFileAsync(@filePath, jsonData);
		}

		/// <summary>List Helper method to write JSON data to local files</summary>
		public static void WriteJSONToFile<T>(string filePath, Object obj)
		{
			WriteJSONToFile(filePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
		}

		/// <summary>List Helper method to write JSON data to local files</summary>
		public static void WriteJSONToFile<T>(string filePath, List<T> listObject)
		{
			try
			{
				WriteJSONToFile(filePath, JsonConvert.SerializeObject(listObject, Formatting.Indented));
			}
			catch (Exception ex)
			{
				using (TextWriter writer = File.CreateText(filePath))
				{
					var serializer = new JsonSerializer();
					serializer.Serialize(writer, listObject);
				}
				throw ex;
			}
		}
	}
}
