﻿using Npgsql;
using QLSV.DAO;
using QLSV.DTO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace QLSV
{
    public partial class StudentForm : Form
    {
		DataTable dt;
		DataTable dtInfo;
		public StudentForm(string id)
        {
            InitializeComponent();
			dt = new DataTable();
			dtInfo = new DataTable();
			ID = id;
            LoadInfo();
            LoadTKB();
			LoadScore();
			OpenRegistration();
			LoadCourseRegistration();
			LoadCourseRegistrationInfo();
			lv_Score.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.ColumnContent);
			
		}

        public string ID { get; set; }

        #region Method
        void LoadInfo()
        {
            StudentInfo info = StudentInfoDAO.Instance.LoadStudentInfo(ID);
            lb_ID.Text = info.Id;
            lb_Name.Text = info.Name;
            lb_Birthday.Text = info.Birthday != new DateTime() ? info.Birthday.ToString("MM/dd/yyyy") : "";
            lb_Gender.Text = info.Gender;
            lb_educationLevel.Text = info.EducationLevel;
            lb_TrainingSystem.Text = info.TrainingSystem;
			Image avt = ConvertBytesToImage(info.Avatar);
            pbx_avt.Image = avt;
        }

		void LoadScore()
		{
			List<StudentScore> data = StudentScoreDAO.Instance.LoadStudentScore(ID);
			if (data.Count != 0)
			{
				Dictionary<string, List<string>> scores = new Dictionary<string, List<string>>();

				foreach (StudentScore item in data)
				{
					string key = item.Semester + "|" + item.SchoolYear;
					string scoreInfo = $"{item.CourseId}|{item.CourseName}|{item.NumberOfCredits}|{item.ProcessScore}|{item.MidtermScore}|{item.PracticeScore}|{item.FinalScore}|{item.CourseScore}";
					if (scores.ContainsKey(key))
					{
						scores[key].Add(scoreInfo);
					}
					else
					{
						scores[key] = new List<string> { scoreInfo };
					}
				}

				foreach (KeyValuePair<string, List<string>> item in scores)
				{
					int totalCredits = 0;
					float totalScore = 0;
					string[] key = item.Key.Split('|').ToArray();
					ListViewItem header = new ListViewItem();
					header.SubItems.Add(key[0]);
					header.SubItems.Add(key[1]);
					lv_Score.Items.Add(header);
					List<string> values = item.Value;
					int i = 1;
					bool hasNull = false;
					foreach (string str in values)
					{
						string[] info = str.Split('|').ToArray();
						List<lectureRatioScore> ratioscores = lectureRatioScoreDAO.Instance.LoadRatioCourse(info[0]);
						ListViewItem listItem = new ListViewItem(i.ToString());

						for (int j = 0; j < info.Length; j++)
						{
							if ((info[j] == "-1") && (j == info.Length - 1)) {
								hasNull = true;
								listItem.SubItems.Add("");
							} else if (info[j] == "-1") {
								listItem.SubItems.Add("");
							} else {
								float isScore;
								if (float.TryParse(info[j], out isScore)) {
									if ((j == 3 && ratioscores[0].RatioProcessScore == 0) || (j == 4 && ratioscores[0].RatioMidtermScore == 0) || (j == 5 && ratioscores[0].RatioPracticeScore == 0) || (j == 6 && ratioscores[0].RatioFinalScore == 0)) {
										listItem.SubItems.Add("");
									} else listItem.SubItems.Add(isScore.ToString("0.##"));
								} else {
									listItem.SubItems.Add(info[j]);
								}
							}
						}
						totalCredits += int.Parse(info[2]);
						if (!hasNull)
						{
							totalScore += float.Parse(info[info.Length - 1]) * int.Parse(info[2]);
						}
						lv_Score.Items.Add(listItem);
						i++;
					}
					ListViewItem general = new ListViewItem();
					general.SubItems.Add("");
					general.SubItems.Add("Trung bình học kỳ");
					general.SubItems.Add(!hasNull ? totalCredits.ToString() : "0");
					general.SubItems.Add("");
					general.SubItems.Add("");
					general.SubItems.Add("");
					general.SubItems.Add("");
					general.SubItems.Add(!hasNull ? (totalScore / totalCredits).ToString("0.##") : "0");
					lv_Score.Items.Add(general);
				}
			}
			else
			{
				ListViewItem header = new ListViewItem();
				header.SubItems.Add("");
				header.SubItems.Add("Chưa có thông tin điểm");
				lv_Score.Items.Add(header);
			}
		}

		void OpenRegistration() {
			string query = "SELECT * FROM GetListRegistrationPeriod()";
			DataTable period = DataProvider.Instance.ExcuteQuery(query);
			DateTime startDate = Convert.ToDateTime(period.Rows[0]["Bắt đầu đăng kí học phần"]);
			if (DateTime.Now < startDate) {
				panel_registrationTool.Visible = false;
				data_CourseRegistration.Visible = false;
				lb_notification.Visible = true;
				panel_cancelTool.Visible = false;
				lb_noti2.Visible = true;
				data_RegistrationInfo.Visible = false;
			}
		}

		void LoadCourseRegistration()
		{
			dt = new DataTable();
			List<StudentCourseRegistration> courses = StudentCourseRegistrationDAO.Instance.LoadStudentCourseRegistration(ID);
			dt.Columns.AddRange(new DataColumn[11] { new DataColumn("Tên môn học", typeof(string)),
						new DataColumn("Mã môn học", typeof(string)),
						new DataColumn("Tên giảng viên",typeof(string)),new DataColumn("Số tín",typeof(int)),new DataColumn("Thứ",typeof(string)),new DataColumn("Tiết",typeof(string)),new DataColumn("Phòng",typeof(string)),new DataColumn("Học kì",typeof(string)),new DataColumn("Năm học",typeof(string)),new DataColumn("Ngày bắt đầu",typeof(DateTime)), new DataColumn("Ngày kết thúc",typeof(DateTime)) });
			// Lỗi
			/*for (int i = courses.Count - 1; i >= 0; i--) {
				foreach (var course in registeredCourseLists) {
					if (courses[i].CourseId == course.CourseId) {
						courses.RemoveAt(i);
					}
				}
			}*/

			// Kiểm tra nếu không quá thời gian ĐKHP thì load ds môn, ngược lại thì ẩn và thông báo cho sinh viên
			// đồng thời tắt tính năng hủy đăng ký ở tab Thông tin ĐKHP
			if (courses != new List<StudentCourseRegistration>()) {
				foreach (StudentCourseRegistration course in courses) {
					dt.Rows.Add(course.CourseName, course.CourseId, course.LecturerName, course.NumberOfCredits, course.Day, course.Period, course.ClassRoom, course.Semester, course.SchoolYear, course.StartDate.ToString("MM/dd/yyyy"), course.EndDate.ToString("MM/dd/yyyy"));
				}
				this.data_CourseRegistration.DataSource = dt;
				data_CourseRegistration.Columns[0].ReadOnly = false;
				for (int k = 1; k < data_CourseRegistration.Columns.Count; k++) {
					data_CourseRegistration.Columns[k].ReadOnly = true;
				}
				this.data_CourseRegistration.AllowUserToAddRows = false;
			} else {
				panel_registrationTool.Visible = false;
				data_CourseRegistration.Visible = false;
				lb_notification.Visible = true;
				btn_CancelRegister.Enabled = false;
			}
		}

		void LoadCourseRegistrationInfo()
		{
			dtInfo = new DataTable();
			dtInfo.Columns.AddRange(new DataColumn[11] { new DataColumn("Tên môn học", typeof(string)),
						new DataColumn("Mã lớp", typeof(string)), new DataColumn("Tên giảng viên", typeof(string)), new DataColumn("Số tín chỉ", typeof(int)), new DataColumn("Thứ", typeof(string)), new DataColumn("Tiết",typeof(string)),new DataColumn("Phòng",typeof(string)),new DataColumn("Học kì",typeof(string)),new DataColumn("Năm học",typeof(string)),new DataColumn("Ngày bắt đầu",typeof(DateTime)), new DataColumn("Ngày kết thúc",typeof(DateTime)) }) ;
			List<RegisteredCourseList> registeredCourseLists = RegisteredCourseListDAO.Instance.LoadRegisteredCourseList(ID);
			foreach (var course in registeredCourseLists)
			{
				dtInfo.Rows.Add(course.CourseName, course.CourseId, course.LecturerName, course.NumberOfCredits, course.Day, course.Period, course.ClassRoom, course.Semester, course.SchoolYear, course.StartDate, course.EndDate);
			}
			this.data_RegistrationInfo.DataSource = dtInfo;
			data_RegistrationInfo.Columns[0].ReadOnly = false;
			for (int k = 1; k < data_RegistrationInfo.Columns.Count; k++)
			{
				data_RegistrationInfo.Columns[k].ReadOnly = true;
			}
			this.data_RegistrationInfo.AllowUserToAddRows = false;
		}

		private void LoadTKB()
		{
			List<StudentSchedule> schedules = StudentScheduleDAO.Instance.LoadStudentSchedule(ID);

			int cellWidth = 125;
			int cellHeight = 45;

			string[] timeline = { "7:30 - 8:15", "8:15 - 9:00", "9:00 - 9:45", "10:00 - 10:45", "10:45 - 11:30", "13:00 - 13:45", "13:45 - 14:30", "14:30 - 15:15", "15:30 - 16:15", "16:15 - 17:00" };

			int courseIndex = 0;

			for (int col = 1; col <= 7; col++)
			{
				for (int row = 0; row <= 10; row++)
				{
					Label lb = new Label
					{
						Width = cellWidth,
						Height = cellHeight,

						BorderStyle = BorderStyle.FixedSingle,

						Margin = new Padding(0),
						TextAlign = ContentAlignment.MiddleCenter,

					};

					flpSchedule.Controls.Add(lb);


					if (row == 0 && col == 1)
					{
						lb.Text = "Thứ/Tiết";
					}
					else if (row == 0)
					{
						lb.Text = $"Thứ {col}";
					}
					else if (col == 1)
					{
						lb.Text = $"Tiết {row}\n({timeline[row - 1]})";

					}

					if (col == 1)
					{
						lb.Width = 130;
					}


					if (courseIndex < schedules.Count)
					{
						StudentSchedule schedule = schedules[courseIndex];
						int day = Convert.ToInt32(schedule.Day);
						int startPeriod = schedule.Period[0] - '0';

						if (day == col && startPeriod == row)
						{
							lb.Text = schedule.ToString();
							lb.BackColor = Color.White;

							lb.Height = cellHeight * schedule.Period.Length;
							row = startPeriod + schedule.Period.Length - 1;
							courseIndex++;
						}
					}
				}
			}
		}
		#endregion

		private void tb_Filter_TextChanged(object sender, EventArgs e)
		{
			dt.DefaultView.RowFilter = string.Format("[Tên môn học] like '%{0}%' or [Mã môn học] like '%{0}%'", tb_Filter.Text);
		}

		private void btn_Register_Click(object sender, EventArgs e)
		{
			bool isAnyChecked = false;
			foreach (DataGridViewRow row in data_CourseRegistration.Rows)
			{
				if (Convert.ToBoolean(row.Cells[0].Value))
				{
					isAnyChecked = true;
					break;
				}
			}
			if (isAnyChecked)
			{
				List<string> registeredCourse = new List<string>();
				List<string> errorCourse = new List<string>();
				for (int i = data_CourseRegistration.Rows.Count - 1; i >= 0; i--)
				{
					DataGridViewRow row = data_CourseRegistration.Rows[i];
					if (Convert.ToBoolean(row.Cells[0].Value))
					{
						string query = "SELECT JoinRegisterCourse( :id , :courseId );";
						bool res = (bool)DataProvider.Instance.ExcuteScalar(query, new object[] { ID, row.Cells[2].Value });
						if (res) {
							registeredCourse.Add(row.Cells[2].Value.ToString());
						} else {
							row.Cells[0].Value = null;
							errorCourse.Add(row.Cells[2].Value.ToString());
						}
					}
				}
				Form bg = new Form();
				RegistrationResult resultWindow = new RegistrationResult(registeredCourse, errorCourse, "register");
				using (resultWindow) {
					bg.StartPosition = FormStartPosition.Manual;
					bg.FormBorderStyle = FormBorderStyle.None;
					bg.BackColor = Color.Black;
					bg.Opacity = 0.7d;
					bg.Size = this.Size;
					bg.Location = this.Location;
					bg.ShowInTaskbar = false;
					bg.Show(this);
					resultWindow.Owner = bg;
					resultWindow.ShowDialog(bg);
					bg.Dispose();
				}
				LoadCourseRegistrationInfo();
				LoadCourseRegistration();
			}
			else
			{
				MessageBox.Show("Vui lòng chọn học phần", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		private void btn_CancelRegister_Click(object sender, EventArgs e)
		{
			bool isAnyChecked = false;
			foreach (DataGridViewRow row in data_RegistrationInfo.Rows)
			{
				if (Convert.ToBoolean(row.Cells[0].Value))
				{
					isAnyChecked = true;
					break;
				}
			}
			if (isAnyChecked)
			{
				List<string> rejectedCourse = new List<string>();
				for (int i = data_RegistrationInfo.Rows.Count - 1; i >= 0; i--)
				{
					DataGridViewRow row = data_RegistrationInfo.Rows[i];
					if (Convert.ToBoolean(row.Cells[0].Value))
					{
						string query = "SELECT LeaveRegisterCourse( :id , :courseId );";
						bool res = (bool)DataProvider.Instance.ExcuteScalar(query, new object[] { ID, row.Cells[2].Value });
						if (res) {
							rejectedCourse.Add(row.Cells[2].Value.ToString());
							dtInfo.Rows.RemoveAt(i);
						}
					}
				}
				Form bg = new Form();
				RegistrationResult resultWindow = new RegistrationResult(rejectedCourse, new List<string>(), "cancel");
				using (resultWindow) {
					bg.StartPosition = FormStartPosition.Manual;
					bg.FormBorderStyle = FormBorderStyle.None;
					bg.BackColor = Color.Black;
					bg.Opacity = 0.7d;
					bg.Size = this.Size;
					bg.Location = this.Location;
					bg.ShowInTaskbar = false;
					bg.Show(this);
					resultWindow.Owner = bg;
					resultWindow.ShowDialog(bg);
					bg.Dispose();
				}
				LoadCourseRegistration();
			} else {
				MessageBox.Show("Vui lòng chọn môn để hủy", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		private void tb_Filter2_TextChanged(object sender, EventArgs e)
		{
			dtInfo.DefaultView.RowFilter = string.Format("[Tên môn học] like '%{0}%' or [Mã lớp] like '%{0}%'", tb_Filter2.Text);
		}

		private void btn_changeAvatar_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG";
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				MessageBox.Show("Cập nhật ảnh thành công!", "Cập nhật ảnh", MessageBoxButtons.OK, MessageBoxIcon.Information);
				pbx_avt.Image = Image.FromFile(openFileDialog.FileName);
				byte[] avt = ConvertImageToBytes(pbx_avt.Image);
				StudentInfo info = StudentInfoDAO.Instance.LoadStudentInfo(ID);
				string query = "SELECT UpdateProfile( :id , :name , :birthday , :gender , :trainingSystem , :educationLevel , :avt );";
				DataProvider.Instance.ExcuteNonQuery(query, new object[] { info.Id, info.Name, info.Birthday, info.Gender, info.TrainingSystem, info.EducationLevel, avt });
				Refresh();
			}
		}

		public byte[] ConvertImageToBytes(Image img)
		{
			if (img == null) { return null; }

			using (MemoryStream ms = new MemoryStream())
			{
				img.Save(ms, ImageFormat.Png);
				return ms.ToArray();
			}
		}

		public Image ConvertBytesToImage(byte[] data)
		{
			if (data != null)
			{
				using (MemoryStream ms = new MemoryStream(data))
				{
					return Image.FromStream(ms);
				}
			}
			Image img = Image.FromFile(@"../../Resources/null_avt.png");
			return img;
		}

		private void lv_Score_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
		{
			e.Cancel = true;
			e.NewWidth = lv_Score.Columns[e.ColumnIndex].Width;
		}

		private void StudentForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			Form bg = new Form();
			CloseWindow logOut = new CloseWindow();
			using (logOut) {
				bg.StartPosition = FormStartPosition.Manual;
				bg.FormBorderStyle = FormBorderStyle.None;
				bg.BackColor = Color.Black;
				bg.Opacity = 0.7d;
				bg.Size = this.Size;
				bg.Location = this.Location;
				bg.ShowInTaskbar = false;
				bg.Show(this);
				logOut.Owner = bg;
				logOut.ShowDialog(bg);
				bg.Dispose();
			}
			e.Cancel = logOut.IsNotClosed;
		}

		private void btn_changePassword_Click(object sender, EventArgs e) {
			ChangePassword changePassword = new ChangePassword(ID);
			Form bg = new Form();
			using (changePassword) {
				bg.StartPosition = FormStartPosition.Manual;
				bg.FormBorderStyle = FormBorderStyle.None;
				bg.BackColor = Color.Black;
				bg.Opacity = 0.7d;
				bg.Size = this.Size;
				bg.Location = this.Location;
				bg.ShowInTaskbar = false;
				bg.Show(this);
				changePassword.Owner = bg;
				changePassword.ShowDialog(bg);
				bg.Dispose();
			}
		}
	}
}
