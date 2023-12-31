﻿using QLSV.DAO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QLSV {
	public partial class SetCourseRegistrationPeriod : Form {
		public SetCourseRegistrationPeriod() {
			InitializeComponent();
			string query = "SELECT * FROM GetListRegistrationPeriod()";
			DataTable period = DataProvider.Instance.ExcuteQuery(query);
			dtp_startDate.Value = Convert.ToDateTime(period.Rows[0]["Bắt đầu đăng kí học phần"]);
			dtp_endDate.Value = Convert.ToDateTime(period.Rows[0]["Kết thúc đăng kí học phần"]);
		}

		private void btn_setPeriod_Click(object sender, EventArgs e) {
			string query = "SELECT updateRegistrationPeriod( :startDate , :endDate )";
			bool res = (bool)DataProvider.Instance.ExcuteScalar(query, new object[] { dtp_startDate.Value, dtp_endDate.Value});
			if (!res) {
				MessageBox.Show("Thời gian đăng ký không hợp lệ, vui lòng đặt lại", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
			} else {
				MessageBox.Show("Đặt thời gian thành công", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
				this.Close();
			}
		}
	}
}
