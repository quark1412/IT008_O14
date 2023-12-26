﻿using OfficeOpenXml;
using PasswordGenerator;
using QLSV.DAO;
using QLSV.DTO;
using Spire.Xls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace QLSV
{
    public partial class ManagerForm : Form
    {
        DataTable dtCourseOfStudent;
        DataTable courses;
        DataTable studentList;

        public ManagerForm(string id)
        {
            InitializeComponent();
            LoadAccountList();
            LoadAllCourse();
            LoadStudentList();
            LoadStudentRegistrationList(data_studentList.Rows[0].Cells[0].Value.ToString());
            LoadListProfileInfo();
            ID = id;
        }
        public string ID { get; set; }

        #region ĐKHP
        void LoadAllCourse()
        {
            string query = "SELECT * FROM GetListRegisterCourse()";
            courses = DataProvider.Instance.ExcuteQuery(query);
            data_allCourse.DataSource = courses;
        }

        void LoadStudentList()
        {
            string query = "SELECT * FROM getListStudents()";
            studentList = DataProvider.Instance.ExcuteQuery(query);
            data_studentList.DataSource = studentList;
        }

        private void btn_import_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "Excel Files|*.xls;*.xlsx;*.xlsm";
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                Workbook workbook = new Workbook();
                workbook.LoadFromFile(openFile.FileName);
                Worksheet worksheet = workbook.Worksheets[0];

                DataTable dt = worksheet.ExportDataTable();

                data_allCourse.DataSource = dt;
            }
        }

        private void btn_export_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Filter = "Excel Files (*.xlsx)|*.xlsx;*.xls";
            if (data_allCourse.Rows.Count > 0)
            {
                if (saveFile.ShowDialog() == DialogResult.OK)
                {
                    using (ExcelPackage pck = new ExcelPackage(new FileInfo(saveFile.FileName)))
                    {
                        ExcelWorksheet ws = pck.Workbook.Worksheets.Add("Sheet1");
                        for (int i = 0; i < data_allCourse.Columns.Count; i++)
                        {
                            ws.Cells[1, i + 1].Value = data_allCourse.Columns[i].HeaderText.ToUpper();
                        }

                        for (int i = 0; i < (data_allCourse.Rows.Count); i++)
                        {
                            for (int j = 0; j < data_allCourse.Columns.Count; j++)
                            {
                                if (data_allCourse.Rows[i].Cells[j].Value != null)
                                {
                                    ws.Cells[i + 2, j + 1].Value = data_allCourse.Rows[i].Cells[j].Value.ToString();
                                }
                            }
                        }
                        pck.Save();
                    }
                }
            }
        }

        private void LoadStudentRegistrationList(string id)
        {
            dtCourseOfStudent = new DataTable();
            dtCourseOfStudent.Columns.AddRange(new DataColumn[11] { new DataColumn("Tên môn học", typeof(string)),
                        new DataColumn("Mã môn học", typeof(string)),
                        new DataColumn("Tên giảng viên",typeof(string)),new DataColumn("Số tín",typeof(int)),new DataColumn("Thứ",typeof(string)),new DataColumn("Tiết",typeof(string)),new DataColumn("Phòng",typeof(string)),new DataColumn("Học kì",typeof(string)),new DataColumn("Năm học",typeof(string)),new DataColumn("Ngày bắt đầu",typeof(DateTime)), new DataColumn("Ngày kết thúc",typeof(DateTime)) });
            List<RegisteredCourseList> registeredCourseLists = RegisteredCourseListDAO.Instance.LoadRegisteredCourseList(id);
            foreach (var course in registeredCourseLists)
            {
                dtCourseOfStudent.Rows.Add(course.CourseName, course.CourseId, course.LecturerName, course.NumberOfCredits, course.Day, course.Period, course.ClassRoom, course.Semester, course.SchoolYear, course.StartDate, course.EndDate);
            }
            data_courseListOfStudent.DataSource = dtCourseOfStudent;
            data_courseListOfStudent.Columns[0].ReadOnly = false;
            foreach (DataGridViewRow row in data_courseListOfStudent.Rows)
            {
                row.Cells[0].Value = true;
            }
            data_courseListOfStudent.Refresh();
            for (int k = 1; k < data_courseListOfStudent.Columns.Count; k++)
            {
                data_courseListOfStudent.Columns[k].ReadOnly = true;
            }
        }

        private void btn_modify_Click(object sender, EventArgs e)
        {
            DataGridViewRow modifyRow = data_allCourse.CurrentRow;
            ModifyCourse modifyCourse = new ModifyCourse(modifyRow);
            Form bg = new Form();
            using (modifyCourse)
            {
                bg.StartPosition = FormStartPosition.Manual;
                bg.FormBorderStyle = FormBorderStyle.None;
                bg.BackColor = Color.Black;
                bg.Opacity = 0.7d;
                bg.Size = this.Size;
                bg.Location = this.Location;
                bg.ShowInTaskbar = false;
                bg.Show(this);
                modifyCourse.Owner = bg;
                modifyCourse.ShowDialog(bg);
                bg.Dispose();
            }
        }

        private void data_courseListOfStudent_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            foreach (DataGridViewRow row in data_courseListOfStudent.Rows)
            {
                DataGridViewCheckBoxCell checkBox = (DataGridViewCheckBoxCell)row.Cells[0];
                checkBox.Value = true;
            }
        }

        private void data_studentList_SelectionChanged(object sender, EventArgs e)
        {
            DataGridViewRow curRow = data_studentList.CurrentRow;
            if (curRow != null)
            {
                string studentId = curRow.Cells[0].Value.ToString();
                tb_studentId.Text = curRow.Cells[0].Value.ToString();
                tb_studentName.Text = curRow.Cells[1].Value.ToString();
                LoadStudentRegistrationList(studentId);
            }
        }

        private void btn_accept_Click(object sender, EventArgs e)
        {
            List<string> accept = new List<string>();
            List<string> reject = new List<string>();
            foreach (DataGridViewRow row in data_courseListOfStudent.Rows)
            {
                if (Convert.ToBoolean(row.Cells[0].Value))
                {
                    string query = "SELECT AcceptCourse( :studentId , :courseId )";
                    bool res = (bool)DataProvider.Instance.ExcuteScalar(query, new object[] { tb_studentId.Text, row.Cells[2].Value });
                    if (res)
                    {
                        accept.Add(row.Cells[2].Value.ToString());
                    }
                    else
                    {
                        string queryReject = "SELECT RejectCourse( :studentId , :courseId )";
                        DataProvider.Instance.ExcuteScalar(queryReject, new object[] { tb_studentId.Text, row.Cells[2].Value });
                        reject.Add(row.Cells[2].Value.ToString());
                    }
                }
                else
                {
                    string query = "SELECT RejectCourse( :studentId , :courseId )";
                    DataProvider.Instance.ExcuteScalar(query, new object[] { tb_studentId.Text, row.Cells[2].Value });
                    reject.Add(row.Cells[2].Value.ToString());
                }
            }
            LoadStudentRegistrationList(data_studentList.CurrentRow.Cells[0].Value.ToString());
            Form bg = new Form();
            RegistrationResult resultWindow = new RegistrationResult(accept, reject, "accept");
            using (resultWindow)
            {
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
        }

        private void tb_findStudent_TextChanged(object sender, EventArgs e)
        {
            studentList.DefaultView.RowFilter = string.Format("[MSSV] like '%{0}%' or [Họ tên] like '%{0}%'", tb_findStudent.Text);
        }

        private void btn_openRegistration_Click(object sender, EventArgs e)
        {
            SetCourseRegistrationPeriod setPeriodWindow = new SetCourseRegistrationPeriod();
            Form bg = new Form();
            using (setPeriodWindow)
            {
                bg.StartPosition = FormStartPosition.Manual;
                bg.FormBorderStyle = FormBorderStyle.None;
                bg.BackColor = Color.Black;
                bg.Opacity = 0.7d;
                bg.Size = this.Size;
                bg.Location = this.Location;
                bg.ShowInTaskbar = false;
                bg.Show(this);
                setPeriodWindow.Owner = bg;
                setPeriodWindow.ShowDialog(bg);
                bg.Dispose();
            }
        }

        #endregion

        #region Tài khoản

        DataTable accountsList;
        string curUsername;
        string curIdProfile;
        string curRole;

        private void LoadAccountList()
        {
            string query = "select * from getListAccounts()";
            accountsList = DataProvider.Instance.ExcuteQuery(query);
            data_accountsList.DataSource = accountsList;

            LoadRoles();
        }

        private void LoadRoles()
        {
            Dictionary<string, string> comboSource = new Dictionary<string, string>();
            comboSource.Add("student", "Sinh viên");
            comboSource.Add("teacher", "Giảng viên");

            cb_role.DataSource = new BindingSource(comboSource, null);
            cb_role.DisplayMember = "Value";
            cb_role.ValueMember = "Key";
        }

        private void data_accountsList_SelectionChanged(object sender, EventArgs e)
        {
            DataGridViewRow currRow = data_accountsList.CurrentRow;
            if (currRow != null)
            {
                tb_username.Text = currRow.Cells[0].Value.ToString();
                tb_idProfile.Text = currRow.Cells[1].Value.ToString();
                cb_role.SelectedValue = currRow.Cells[2].Value.ToString();
                tb_passGenarator.Text = string.Empty;
            }
        }

        string HashedPassword(string password)
        {
            return BCrypt.Net.BCrypt.EnhancedHashPassword(password, 12);
        }

        private void btn_resetPass_Click(object sender, EventArgs e)
        {
            string newPassword = new Password(8).Next();
            tb_passGenarator.Text = newPassword;

            string username = tb_username.Text;
            string hashPassword = HashedPassword(newPassword);

            string query = "SELECT UpdatePass( :username , :password )";
            bool success = (bool)DataProvider.Instance.ExcuteScalar(query, new object[] { username, hashPassword });

            if (success)
            {
                MessageBox.Show($"Đặt lại mật khẩu thành công!\nGiả lập: password mới {newPassword} đã được gửi đến username: {username}", "Đặt lại mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Đặt lại mật khẩu khẩu", "Đặt lại mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EnableButton(bool editing = false)
        {
            btn_createAccount.Enabled = !editing;
            btn_resetPass.Enabled = !editing;

            btn_acceptAccount.Enabled = editing;
            btn_Cancel.Enabled = editing;
            tb_username.Enabled = editing;
            tb_idProfile.Enabled = editing;
            cb_role.Enabled = editing;
        }


        private void btn_createAccount_Click(object sender, EventArgs e)
        {
            curUsername = tb_username.Text;
            curIdProfile = tb_idProfile.Text;
            curRole = cb_role.SelectedValue.ToString();

            EnableButton(true);

            tb_username.Text = "";
            tb_idProfile.Text = "";
            string newPassword = new Password(8).Next();
            tb_passGenarator.Text = newPassword;
        }

        private void btn_acceptAccount_Click(object sender, EventArgs e)
        {
            string hashPassword = HashedPassword(tb_passGenarator.Text);

            string query = "SELECT InsertAcc( :username , :password , :role , :idProfile )";

            bool success = (bool)DataProvider.Instance.ExcuteScalar(query, new object[] { tb_username.Text, hashPassword, cb_role.SelectedValue.ToString(), tb_idProfile.Text });

            if (success)
            {
                MessageBox.Show("Tạo mới tài khoản thành công");
                LoadAccountList();
                EnableButton(false);
            }
            else
            {
                MessageBox.Show("Thất bại! Tên đăng nhập / MSSV / MGV đã tồn tại");
            }
        }

        #endregion

        #region Thông tin sinh viên

        DataTable profileList;

        private void LoadListProfileInfo()
        {
            string query = "SELECT * FROM GetListProfileInfo()";
            profileList = DataProvider.Instance.ExcuteQuery(query);
            data_profileList.DataSource = profileList;
        }


        private void btn_Cancel_Click(object sender, EventArgs e)
        {
            tb_username.Text = curUsername;
            tb_idProfile.Text = curIdProfile;
            cb_role.SelectedValue = curRole;

            EnableButton(false);
            tb_passGenarator.Text = "";
        }

        private void btn_editProfile_Click(object sender, EventArgs e)
        {
            DataGridViewRow modifyRow = data_profileList.CurrentRow;
            ModifyProfile modifyProfile = new ModifyProfile(modifyRow.Cells[0].Value.ToString());

            Form bg = new Form();
            using (modifyProfile)
            {
                bg.StartPosition = FormStartPosition.Manual;
                bg.FormBorderStyle = FormBorderStyle.None;
                bg.BackColor = Color.Black;
                bg.Opacity = 0.7d;
                bg.Size = this.Size;
                bg.Location = this.Location;
                bg.ShowInTaskbar = false;
                bg.Show(this);
                modifyProfile.Owner = bg;
                modifyProfile.ShowDialog(bg);
                bg.Dispose();
            }

            LoadListProfileInfo();
        }

        #endregion

        private void ManagerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Form bg = new Form();
            CloseWindow logOut = new CloseWindow();
            using (logOut)
            {
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

    }
}
