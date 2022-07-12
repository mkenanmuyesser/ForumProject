using System;
using System.Collections.Generic;
using System.Web;
using System.Data.Common;
using System.IO;
using System.Data;

namespace aspnetforum.Utils
{
	public static class Attachments
	{
		private static string GetIconImageByFileName(string filename)
		{
			string ext = filename.Substring(filename.LastIndexOf(".") + 1).ToLower();
			string iconImage = "fileicon_na.png";
			switch (ext)
			{
				case "pdf":
					iconImage = "fileicon_pdf.png";
					break;
				case "doc":
					iconImage = "fileicon_word.png";
					break;
				case "xls":
				case "csv":
					iconImage = "fileicon_excel.png";
					break;
			}
			return iconImage;
		}

		public static void DeleteTopicAttachments(int topicId, DbConnection cn)
		{
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			var files = cn.ExecuteData(
				"SELECT FileName, UserID FROM ForumUploadedFiles WHERE MessageID IN (SELECT MessageID FROM ForumMessages WHERE TopicID=?)",
				topicId);

			foreach (var file in files)
			{
				DeleteAttachmentFile(Convert.ToInt32(file["UserID"]), file["FileName"].ToString());
			}
			
			cn.ExecuteNonQuery(
				"DELETE FROM ForumUploadedFiles WHERE MessageID IN (SELECT MessageID FROM ForumMessages WHERE TopicID=?)",
				topicId);
			
			if (!openConn) cn.Close();
		}

		/// <summary>
		/// get image thunbnail for image-type files
		/// </summary>
		/// <param name="filename">filename</param>
		/// <param name="userID">user who attached the file</param>
		/// <returns></returns>
		public static string GetThumbnail(string filename, int userID)
		{
			if (filename == null) return "";

			string lowFilename = filename.ToLower();

			if (lowFilename.EndsWith("gif") || lowFilename.EndsWith("jpg") || lowFilename.EndsWith("jpeg") || lowFilename.EndsWith("png"))
			{
				//it's an image
				string src = "imgthumbnail.ashx?Image=" + userID.ToString() + "\\" + filename;
				return "<img class=\"avatar\" alt=\"\" src=\"" + src + "\" />";
			}
			else //its not a image lets show the filetype icon
				return "<img alt=\"\" src=\"images/" + GetIconImageByFileName(filename) + "\" />";
		}

		public static bool CheckAttachmentsSize()
		{
			int maxSize = Settings.MaxUploadFileSize;
			//loop through the posted files
			HttpFileCollection files = HttpContext.Current.Request.Files;
			for (int i = 0; i < files.Count; i++)
			{
				//max size check
				if (files[i].ContentLength > maxSize)
				{
					return false;
				}
			}
			return true;
		}

		public static void DeleteMessageAttachmentById(int fileId)
		{
			DbCommand Cmd = Utils.DB.CreateCommand();
			Cmd.Connection.Open();
			Cmd.CommandText = "SELECT FileName, UserID FROM ForumUploadedFiles WHERE FileID=" + fileId;
			DbDataReader dr = Cmd.ExecuteReader();
			if (dr.Read())
			{
				DeleteAttachmentFile(Convert.ToInt32(dr["UserID"]), dr["FileName"].ToString());
			}
			dr.Close();
			Cmd.CommandText = "DELETE FROM ForumUploadedFiles WHERE FileID=" + fileId;
			Cmd.ExecuteNonQuery();
			Cmd.Connection.Close();
		}

		private static void DeleteAttachmentFile(int userId, string fileName)
		{
			string folderPath = GetUploadDirAbsolutePath() + userId + "\\";
			string folderPathOldVersion = GetUploadDirAbsolutePathOLDVersion() + userId + "\\";
			
			string filepath = folderPath + fileName;
			if (File.Exists(filepath)) File.Delete(filepath);
			else //if old version
			{
				filepath = folderPathOldVersion + fileName;
				if (File.Exists(filepath)) File.Delete(filepath);
			}

			//cleanup

			if (Directory.Exists(folderPath) && Directory.GetFiles(folderPath).Length == 0)
			{
				Directory.Delete(folderPath);
			}

			if (Directory.Exists(folderPathOldVersion) && Directory.GetFiles(folderPathOldVersion).Length == 0)
			{
				Directory.Delete(folderPathOldVersion);
			}
		}

		private static void UnitTest()
		{
			string tst = GetContentType("test.jpg");
			tst = GetContentType("test.jpeg");
		}

		public static string GetContentType(string filename)
		{
			string ext = filename.Substring(filename.LastIndexOf(".") + 1);
			switch (ext)
			{
				case "png": return "image/png";
				case "jpg": return "image/jpeg";
				case "jpeg": return "image/jpeg";
				case "gif": return "image/gif";
				default: return "application/octet-stream";
			}
		}

		public static void DeleteMessageAttachments(int msgid, bool isPersonalMessage, DbConnection cn)
		{
			string tblName = isPersonalMessage ? "ForumUploadedPersonalFiles" : "ForumUploadedFiles";

			var dr = cn.ExecuteReader(string.Format("SELECT FileName, UserID FROM {0} WHERE MessageID={1}", tblName, msgid));
			while (dr.Read())
			{
				DeleteAttachmentFile(Convert.ToInt32(dr["UserID"]), dr["FileName"].ToString());
			}
			dr.Close();
			cn.ExecuteNonQuery(string.Format("DELETE FROM {0} WHERE MessageID={1}", tblName, msgid));
		}

		public static void SaveAttachments(int msgid, bool isPersonalMessage, DbConnection cn)
		{
			//to save performance and prevent close/reopn connection a thousand times we do this
			bool openConn = (cn.State == ConnectionState.Open);
			if (!openConn) cn.Open();

			HttpFileCollection files = HttpContext.Current.Request.Files;
			if (files.Count < 1) return;

			//create a folder, named "1234" where "1234" is the user's id
			string uploadDir = GetUploadDirAbsolutePath() + User.CurrentUserID.ToString();

			//loop through the posted files
			for (int i = 0; i < files.Count; i++)
			{
				//empty filename check
				if (files[i].FileName.Trim() == "") continue;

				string shortname = Path.GetFileName(files[i].FileName);

				//if extension forbidden
				if (IsExtForbidden(shortname)) continue;

				if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

				//rename if file already exists
				shortname = ChangeFileNameIfAlreadyExists(shortname, uploadDir);

				//save file to disk
				files[i].SaveAs(uploadDir + "\\" + shortname);

				//write to db
				string tblName = isPersonalMessage ? "ForumUploadedPersonalFiles" : "ForumUploadedFiles";
				cn.ExecuteNonQuery(string.Format("INSERT INTO {0} (FileName, MessageID, UserID) VALUES (?, ?, ?)", tblName)
					, shortname, msgid, User.CurrentUserID);
			}

			if (!openConn) cn.Close();
		}

		public static bool IsExtForbidden(string filename)
		{
			int dotPos = filename.LastIndexOf(".");
			string ext = filename.Substring(dotPos + 1);
			
			foreach (string forbiddenExt in Settings.ForbiddenUploadExtensions)
				if (ext == forbiddenExt) return true;

			//disallow asp.net extensions - to prevent possible hijacking in the upload folder
			if (ext == "aspx" || ext == "asmx" || ext == "ashx" || ext == "asax" || ext == "ascx" || ext == "axd" || ext == "asp" || ext == "config")
				return true;
			
			return false;
		}

		//if a file with that name already exists, reuturns new filename in the form "filename[2].ext" or "filename[3].ext"
		public static string ChangeFileNameIfAlreadyExists(string shortFileName, string uploadDir)
		{
			//rename if the file already exists
			int i = 0;
			int dotPos = shortFileName.LastIndexOf(".");
			string namewithoutext = shortFileName.Substring(0, dotPos);
			string ext = shortFileName.Substring(dotPos + 1);
			while (File.Exists(Path.Combine(uploadDir, shortFileName)))
			{
				i = i + 1;
				shortFileName = namewithoutext + "[" + i + "]." + ext;
			}
			return shortFileName;
		}

		public static void GetAttachment(int fileId, bool isPrivateMessage, out int userId, out string fileName)
		{
			using (var cn = DB.CreateOpenConnection())
			{
				int forumId = 0;
				userId = 0;
				fileName = "";
				string sql;

				if (isPrivateMessage)
				{
					sql = @"SELECT 0 as ForumID, ForumUploadedPersonalFiles.UserID, ForumUploadedPersonalFiles.FileName
						FROM ForumUploadedPersonalFiles
						WHERE ForumUploadedPersonalFiles.FileID=?";
				}
				else
				{
					sql = @"SELECT ForumTopics.ForumID, ForumUploadedFiles.UserID, ForumUploadedFiles.FileName
						FROM (ForumMessages INNER JOIN ForumUploadedFiles ON ForumMessages.MessageID = ForumUploadedFiles.MessageID)
						INNER JOIN ForumTopics ON ForumMessages.TopicID = ForumTopics.TopicID
						WHERE ForumUploadedFiles.FileID=?";
				}

				DbDataReader dr = cn.ExecuteReader(sql, fileId);
				if (dr.Read())
				{
					forumId = Convert.ToInt32(dr["ForumID"]);
					userId = Convert.ToInt32(dr["UserID"]);
					fileName = dr["FileName"].ToString();
				}
				dr.Close();
				bool permission = isPrivateMessage ? true : Forum.CheckForumReadPermissions(forumId, Utils.User.CurrentUserID);

				if (!permission)
				{
					throw new AccessViolationException("permission denied");
				}
			}
		}

		//path to forum uploads (App_Data\forumuploadfiles)
		public static string GetUploadDirAbsolutePath()
		{
			string path = Path.Combine(Settings.FilesUploadPath, "forumuploadfiles\\");
			if (!Directory.Exists(path)) Directory.CreateDirectory(path);

			return path;
		}

		//OLD path to forum uploads (App_Data\forumuploadfiles)
		public static string GetUploadDirAbsolutePathOLDVersion()
		{
			string path = HttpContext.Current.Request.MapPath("upload") + "\\";
			return path;
		}

		//path to forum icons (App_Data\forumicons)
		public static string GetIconsDirAbsolutePath()
		{
			string path =  Path.Combine(Settings.FilesUploadPath, "forumicons\\");
			if (!Directory.Exists(path)) Directory.CreateDirectory(path);

			return path;
		}

		//path to forum icons (App_Data\forumavatars)
		public static string GetAvatarsDirAbsolutePath()
		{
			string path = Path.Combine(Settings.FilesUploadPath, "forumavatars\\");
			if (!Directory.Exists(path)) Directory.CreateDirectory(path);

			return path;
		}
	}
}
