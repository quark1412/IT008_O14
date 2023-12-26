﻿using QLSV.DAO;
using QLSV.DTO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace QLSV
{
    public partial class ModifyProfile : Form
    {
        string ID;

        public ModifyProfile(string id)
        {
            InitializeComponent();
            ID = id;
            LoadProfileInfo();
        }

        private void LoadProfileInfo()
        {
            LoadGender();

            StudentInfo info = StudentInfoDAO.Instance.LoadStudentInfo(ID);
            tb_id.Text = info.Id;
            tb_name.Text = info.Name;
            dtp_birthDay.Text = info.Birthday != new DateTime() ? info.Birthday.ToString("MM/dd/yyyy") : "";
            cb_gender.SelectedText = info.Gender;
            tb_educationLevel.Text = info.EducationLevel;
            tb_trainingSystem.Text = info.TrainingSystem;
            Image avt = ConvertBytesToImage(info.Avatar);
            pbx_avt.Image = avt;
        }

        private void LoadGender()
        {
            Dictionary<string, string> comboSource = new Dictionary<string, string>();
            comboSource.Add("male", "Nam");
            comboSource.Add("female", "Nữ");
            comboSource.Add("orther", "Khác");

            cb_gender.DataSource = new BindingSource(comboSource, null);
            cb_gender.DisplayMember = "Value";
            cb_gender.ValueMember = "Key";
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

        public byte[] ConvertImageToBytes(Image img)
        {
            if (img == null) { return null; }

            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        private void btn_changeAvatar_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF";
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

        private void btn_update_Click(object sender, EventArgs e)
        {
            KeyValuePair<string, string> selectedGender = (KeyValuePair<string, string>)cb_gender.SelectedItem;
            string gender = selectedGender.Value;

            byte[] avt = ConvertImageToBytes(pbx_avt.Image);

            string query = "SELECT UpdateProfile( :id , :name , :birthday , :gender , :educationLevel ,  :trainingSystem , :avt );";
            bool success = (bool)DataProvider.Instance.ExcuteScalar(query, new object[] { tb_id.Text, tb_name.Text, dtp_birthDay.Value.Date, gender, tb_trainingSystem.Text, tb_educationLevel.Text, avt });

            if (success)
            {
                MessageBox.Show("Cập nhật thông tin cá nhân thành công!", "Cập nhật thông tin cá nhân", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Thông tin ngày sinh không hợp lệ", "Cập nhật thông tin cá nhân", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


    }
}
