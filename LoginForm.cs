﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Npgsql;
using QLSV.DAO;
using System.Reflection;

namespace QLSV
{
	public partial class LoginForm : Form
	{
		public LoginForm()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			tb_password.PasswordChar = '\u25CF';
		}

		private void btn_exit_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void btn_login_Click(object sender, EventArgs e)
		{
			string userName = tb_userName.Text;
			string password = tb_password.Text;

			if (userName == "")
			{
				MessageBox.Show("Yêu cầu nhập tên đăng nhập!");
				return;
			}

			if (password == "")
			{
				MessageBox.Show("Yêu cầu nhập mật khẩu!");
				return;
			}

			if (Login(userName, password))
			{
				this.Hide();
				string id = GetId(userName, password);
				switch (GetRole(userName, password))
				{
					case "student":
						StudentForm student = new StudentForm(id);
						student.ShowDialog();
						break;
					case "teacher":
						LecturerForm lecturer = new LecturerForm(id);
						lecturer.ShowDialog();
						break;
					case "admin":
						ManagerForm manager = new ManagerForm(id);
						manager.ShowDialog();
						break;
				}
				this.Show();
			}
			else
			{
				MessageBox.Show("Sai tài khoản hoặc mật khẩu!");
			}
		}

		private string GetId(string username, string password)
		{
			return AccountDAO.Instance.GetId(username, password);
		}

		private bool Login(string userName, string password)
		{
			return AccountDAO.Instance.Login(userName, password);
		}
		private string GetRole(string userName, string password)
		{
			return AccountDAO.Instance.GetRole(userName, password);
		}

		private void tb_userName_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter && tb_userName.Text != "")
			{
				tb_password.Focus();
			}
			else if (e.KeyCode == Keys.Enter && tb_userName.Text == "")
			{
				btn_login.PerformClick();
			}
		}

		private void tb_password_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				btn_login.PerformClick();
			}
		}

		private void btn_login_MouseHover(object sender, EventArgs e)
		{
			btn_login.Cursor = System.Windows.Forms.Cursors.Hand;
		}

		private void btn_exit_MouseHover(object sender, EventArgs e)
		{
			btn_exit.Cursor = System.Windows.Forms.Cursors.Hand;
		}
	}
}
