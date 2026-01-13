using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QLQuanCafe
{
    public partial class fAdmin : Form
    {
        //string connection_string_sql = @"Data Source=THINKPADX1\SQLSEVER;Initial Catalog=QLQuanCafe;Integrated Security=True";
        string connection_string_sql = @"Data Source=LAPTOP-S8BTMJ0M;Initial Catalog=QLQuanCafe;Integrated Security=True";
        string _originDisplayName;
        string _originPassword;
        bool _originType;
        string _originTableName;
        bool _originTableStatus;
        private string _originFoodName;
        private decimal _originPrice;
        private object _originCategoryID;

        SqlConnection ketNoi;
        SqlDataAdapter boDocGhi;
        DataSet dsAccount;
        public fAdmin()
        {
            InitializeComponent();
        }        
        private void fAdmin_Load(object sender, EventArgs e)
        {
            LoadGridViewAccount();
            LoadGridViewFoodTable();
            LoadTypeAccount();
            LoadTableStatus();
            LoadGridViewFoodDrink();
            LoadFoodCategory();
            txtTableName.TextChanged += TableField_Changed;
            cbTableStatus.SelectedValueChanged += TableField_Changed;
            dtgvFoodDrink.CellClick += dtgvFoodDrink_CellClick;
            txbFoodName.TextChanged += FoodField_Changed;
            txbPrice.TextChanged += FoodField_Changed;
            cbFoodCategory.SelectedValueChanged += FoodField_Changed;

        }
        void LoadTypeAccount()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Value", typeof(bool));
            dt.Columns.Add("Text", typeof(string));

            dt.Rows.Add(false, "Nhân viên");
            dt.Rows.Add(true, "Admin");

            cbTypeAccount.DataSource = dt;
            cbTypeAccount.DisplayMember = "Text";
            cbTypeAccount.ValueMember = "Value";
        }

        void LoadTableStatus()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Value", typeof(bool));
            dt.Columns.Add("Text", typeof(string));

            dt.Rows.Add(false, "Trống");
            dt.Rows.Add(true, "Có người");

            cbTableStatus.DataSource = dt;
            cbTableStatus.DisplayMember = "Text";
            cbTableStatus.ValueMember = "Value";
        }
        void ClearTableInformation()
        {
            txtIDTable.Text = "";
            txtTableName.Text = "";
            cbTableStatus.SelectedIndex = 0;
            txtIDTable.ReadOnly = false;

            btnEditTable.Text = "Sửa bàn";
        }


        void clearInformation()
        {
            txtUserName.ReadOnly = false;
            txtUserName.Text = "";
            txtDisplayName.Text = "";
            txtPassword.Text = "";
            cbTypeAccount.SelectedIndex = 0;
        }
        void AccountField_Changed(object sender, EventArgs e)
        {
            if (txtUserName.Text == "") return; // chưa chọn account

            bool isChanged =
                txtDisplayName.Text != _originDisplayName ||
                txtPassword.Text != _originPassword ||
                (bool)cbTypeAccount.SelectedValue != _originType;

            btnEditAccount.Text = isChanged ? "Cập nhật" : "Sửa tài khoản";
        }
        void ClearFoodInput()
        {
            txbFoodID.Text = "";
            txbFoodName.Text = "";
            txbPrice.Text = "0";
            cbFoodCategory.SelectedIndex = 0; // Reset combo
            txbFoodID.ReadOnly = false;
            btnEditFoods.Text = "Sửa món"; // Đảm bảo reset
        }
        void LoadFoodCategory()
        {
            using (SqlConnection conn = new SqlConnection(connection_string_sql))
            {
                string query = "SELECT foodCateID, foodCateName FROM FoodCategory";
                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                cbFoodCategory.DataSource = dt;
                cbFoodCategory.DisplayMember = "foodCateName";
                cbFoodCategory.ValueMember = "foodCateID";
            }
        }
        void FoodField_Changed(object sender, EventArgs e)
        {
            if (txbFoodID.Text == "") return; // chưa chọn món
            bool isChanged =
                txbFoodName.Text != _originFoodName ||
                txbPrice.Text != _originPrice.ToString() || // So sánh string vì txbPrice là TextBox
                cbFoodCategory.SelectedValue.ToString() != _originCategoryID.ToString();
            btnEditFoods.Text = isChanged ? "Cập nhật" : "Sửa món";
        }
        bool TryGetFoodPrice(out decimal price)
        {
            price = 0;
            if (string.IsNullOrWhiteSpace(txbPrice.Text))
            {
                MessageBox.Show("Vui lòng nhập giá tiền");
                return false;
            }
            if (!decimal.TryParse(txbPrice.Text.Trim(), out price))
            {
                MessageBox.Show("Giá tiền không hợp lệ (chỉ nhập số)");
                return false;
            }
            if (price < 0)
            {
                MessageBox.Show("Giá tiền phải lớn hơn 0");
                return false;
            }
            return true;
        }
        private void txbPrice_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }
        private void LoadGridViewAccount()
        {
            string connection_string = connection_string_sql;
            ketNoi = new SqlConnection(connection_string);
            string sql = "SELECT * FROM Account";
            boDocGhi = new SqlDataAdapter(sql, ketNoi);
            dsAccount = new DataSet("DSAccount");
            boDocGhi.Fill(dsAccount, "Account");
            dtgvAccount.DataSource = dsAccount.Tables["Account"];

            //doi ten cot
            dtgvAccount.Columns["userName"].HeaderText = "Tên đăng nhập";
            dtgvAccount.Columns["displayName"].HeaderText = "Tên hiển thị";
            dtgvAccount.Columns["PassWord"].HeaderText = "Mật khẩu";
            dtgvAccount.Columns["Type"].HeaderText = "Loại tài khoản";
        }

        private void LoadGridViewFoodTable()
        {
            using (SqlConnection connection = new SqlConnection(connection_string_sql))
            {
                string queryCheckStatusTable = @"SELECT ft.tableID, ft.tableName,
                                                CASE 
                                                    WHEN EXISTS (
                                                        SELECT 1 
                                                        FROM Bill b 
                                                        WHERE b.IDTable = ft.tableID AND b.billStatus = 1
                                                    )
                                                    THEN N'Có Người'
                                                    ELSE N'Trống'
                                                END AS tableStatus
                                                FROM FoodTable ft";

                SqlDataAdapter da = new SqlDataAdapter(queryCheckStatusTable, connection);

                DataTable dtTable = new DataTable();
                da.Fill(dtTable);

                dtgvTable.DataSource = dtTable;
            }

            dtgvTable.Columns["tableID"].HeaderText = "Mã bàn";
            dtgvTable.Columns["tableName"].HeaderText = "Tên bàn";
            dtgvTable.Columns["tableStatus"].HeaderText = "Trạng thái";
        }


        private void dtgvAccount_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dtgvAccount.Rows[e.RowIndex];

            txtUserName.Text = row.Cells["userName"].Value.ToString();
            txtDisplayName.Text = row.Cells["displayName"].Value.ToString();
            txtPassword.Text = row.Cells["PassWord"].Value.ToString();
            cbTypeAccount.SelectedValue = Convert.ToBoolean(row.Cells["Type"].Value);

            txtUserName.ReadOnly = true;

            // LƯU GIÁ TRỊ BAN ĐẦU
            _originDisplayName = txtDisplayName.Text;
            _originPassword = txtPassword.Text;
            _originType = (bool)cbTypeAccount.SelectedValue;

            // reset nút
            btnEditAccount.Text = "Sửa tài khoản";
        }
        private void btnAddAccount_Click(object sender, EventArgs e)
        {

            if (txtUserName.Text.Length <= 1)
            {
                MessageBox.Show("Username phải nhiều hơn 1 ký tự");
                return;
            }
            else
            {
                string qSelect = "SELECT COUNT(*) FROM Account WHERE userName = @userName";
                SqlCommand boLenh = new SqlCommand(qSelect, ketNoi);
                boLenh.Parameters.AddWithValue("@userName", txtUserName.Text);

                try
                {
                    ketNoi.Open();
                    int checkUserName = (int)boLenh.ExecuteScalar();

                    if (checkUserName != 0)
                    {
                        MessageBox.Show("Username đã tồn tại, vui lòng chọn username khác");
                        clearInformation();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi kiểm tra username: " + ex.Message);
                    return;
                }
                finally
                {
                    if (ketNoi.State == ConnectionState.Open)
                        ketNoi.Close();
                }
            }

            if (txtUserName.Text == "")
            {
                MessageBox.Show("Vui lòng nhập Username");
                return;
            }

            if (txtDisplayName.Text == "")
            {
                MessageBox.Show("Vui lòng nhập tên hiển thị");
                return;
            }

            string qAdd = "INSERT INTO Account (userName, PassWord, displayName, type) " +
                          "VALUES (@userName, @PassWord, @displayName, @Type)";
            boDocGhi.InsertCommand = new SqlCommand(qAdd, ketNoi);
            boDocGhi.InsertCommand.Parameters.AddWithValue("@userName", txtUserName.Text);
            boDocGhi.InsertCommand.Parameters.AddWithValue("@PassWord", txtPassword.Text);
            boDocGhi.InsertCommand.Parameters.AddWithValue("@displayName", txtDisplayName.Text);
            boDocGhi.InsertCommand.Parameters.AddWithValue("@Type", cbTypeAccount.SelectedValue);
            try
            {
                ketNoi.Open();
                boDocGhi.InsertCommand.ExecuteNonQuery();
                MessageBox.Show("Thêm tài khoản thành công");
                LoadGridViewAccount();
                clearInformation();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi thêm tài khoản: " + ex.Message);
                return;
            }
            finally
            {
                if (ketNoi.State == ConnectionState.Open)
                    ketNoi.Close();
            }
        }

        private void btnDelAccount_Click(object sender, EventArgs e)
        {
            if (txtUserName.Text == "")
            {
                MessageBox.Show("Vui lòng chọn tài khoản cần xóa");
                return;
            }

            DialogResult rs = MessageBox.Show(
                $"Bạn có chắc muốn xóa tài khoản [{txtUserName.Text}]?",
                "Xác nhận",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

            if (rs == DialogResult.No) return;

            string qDel = "DELETE FROM Account WHERE userName = @userName";
            SqlCommand cmdDel = new SqlCommand(qDel, ketNoi);
            cmdDel.Parameters.AddWithValue("@userName", txtUserName.Text);

            try
            {
                ketNoi.Open();
                int kq = cmdDel.ExecuteNonQuery();

                if (kq > 0)
                {
                    MessageBox.Show("Xóa tài khoản thành công");
                    LoadGridViewAccount();
                    clearInformation();
                    txtUserName.ReadOnly = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kiểm tra tài khoản: " + ex.Message);
                return;
            }
            finally
            {
                if (ketNoi.State == ConnectionState.Open)
                    ketNoi.Close();
            }
        }

        private void btnEditAccount_Click(object sender, EventArgs e)
        {
            if(txtUserName.Text == "")
            {
                MessageBox.Show("Vui lòng chọn tài khoản cần sửa");
                return;
            }

            if(txtDisplayName.Text == "")
            {
                MessageBox.Show("Vui lòng nhập tên hiển thị");
                return;
            }

            DialogResult rs = MessageBox.Show(
                $"Bạn có chắc muốn sửa thông tin tài khoản [{txtUserName.Text}]?",
                "Xác nhận",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

            if (rs == DialogResult.No) return;

            string qUpdate = @"UPDATE Account SET 
                                displayName = @displayName,
                                PassWord = @PassWord,
                                Type = @Type WHERE userName = @userName";
            
            SqlCommand cmdUpdate = new SqlCommand(qUpdate, ketNoi);
            cmdUpdate.Parameters.AddWithValue("@displayName", txtDisplayName.Text);
            cmdUpdate.Parameters.AddWithValue("@PassWord", txtPassword.Text);
            cmdUpdate.Parameters.AddWithValue("@Type", cbTypeAccount.SelectedValue);
            cmdUpdate.Parameters.AddWithValue("@userName", txtUserName.Text);

            try
            {
                ketNoi.Open();
                int kq = cmdUpdate.ExecuteNonQuery();

                if (kq > 0)
                {
                    MessageBox.Show("Cập nhật tài khoản thành công");
                    LoadGridViewAccount();

                    // reset lại trạng thái
                    _originDisplayName = txtDisplayName.Text;
                    _originPassword = txtPassword.Text;
                    _originType = (bool)cbTypeAccount.SelectedValue;

                    btnEditAccount.Text = "Sửa tài khoản";
                    txtUserName.ReadOnly = false;
                    clearInformation();
                }
                else
                {
                    MessageBox.Show("Không tìm thấy tài khoản để cập nhật");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi cập nhật tài khoản: " + ex.Message);
                return;
            }
            finally
            {
                if (ketNoi.State == ConnectionState.Open)
                    ketNoi.Close();
            }
        }

        void TableField_Changed(object sender, EventArgs e)
        {
            if (txtIDTable.Text == "") return;

            bool currentStatus = Convert.ToBoolean(cbTableStatus.SelectedValue);

            bool isChanged =
                txtTableName.Text != _originTableName ||
                currentStatus != _originTableStatus;

            btnEditTable.Text = isChanged ? "Cập nhật" : "Sửa bàn";

        }



        //xu ly table
        private void dtgvTable_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dtgvTable.Rows[e.RowIndex];

            txtIDTable.Text = row.Cells["tableID"].Value.ToString();
            txtTableName.Text = row.Cells["tableName"].Value.ToString();

            bool status = row.Cells["tableStatus"].Value.ToString() == "Có người";
            cbTableStatus.SelectedValue = status;

            txtIDTable.ReadOnly = true;

            // 🔹 LƯU GIÁ TRỊ BAN ĐẦU (GIỐNG ACCOUNT)
            _originTableName = txtTableName.Text;
            _originTableStatus = status;

            // reset nút
            btnEditTable.Text = "Sửa bàn";
        }


        private void btnAddTable_Click(object sender, EventArgs e)
        {
            // 1. Kiểm tra dữ liệu
            if (txtTableName.Text.Trim().Length <= 1)
            {
                MessageBox.Show("Tên bàn phải nhiều hơn 1 ký tự");
                return;
            }

            // 2. Kiểm tra trùng tên bàn
            string qCheck = "SELECT COUNT(*) FROM FoodTable WHERE tableName = @tableName";
            SqlCommand cmdCheck = new SqlCommand(qCheck, ketNoi);
            cmdCheck.Parameters.AddWithValue("@tableName", txtTableName.Text.Trim());

            try
            {
                ketNoi.Open();
                int check = (int)cmdCheck.ExecuteScalar();

                if (check != 0)
                {
                    MessageBox.Show("Tên bàn đã tồn tại, vui lòng nhập tên khác");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kiểm tra tên bàn: " + ex.Message);
                return;
            }
            finally
            {
                if (ketNoi.State == ConnectionState.Open)
                    ketNoi.Close();
            }

            // 3. Thêm bàn (giống Add Account)
            string qAdd = "INSERT INTO FoodTable (tableName, tableStatus) VALUES (@tableName, @tableStatus)";
            boDocGhi = new SqlDataAdapter();
            boDocGhi.InsertCommand = new SqlCommand(qAdd, ketNoi);
            boDocGhi.InsertCommand.Parameters.AddWithValue("@tableName", txtTableName.Text.Trim());
            boDocGhi.InsertCommand.Parameters.AddWithValue("@tableStatus", cbTableStatus.SelectedValue);

            try
            {
                ketNoi.Open();
                boDocGhi.InsertCommand.ExecuteNonQuery();

                MessageBox.Show("Thêm bàn thành công");
                LoadGridViewFoodTable();

                ClearTableInformation();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi thêm bàn: " + ex.Message);
            }
            finally
            {
                if (ketNoi.State == ConnectionState.Open)
                    ketNoi.Close();
            }
        }


        private void btnDelTable_Click(object sender, EventArgs e)
        {
            {
                if (txtIDTable.Text == "")
                {
                    MessageBox.Show("Vui lòng chọn bàn cần xóa");
                    return;
                }

                DialogResult rs = MessageBox.Show(
                    $"Bạn có chắc muốn xóa bàn [{txtTableName.Text}]?",
                    "Xác nhận",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (rs == DialogResult.No) return;

                string qDel = "DELETE FROM FoodTable WHERE tableID = @tableID";
                SqlCommand cmdDel = new SqlCommand(qDel, ketNoi);
                cmdDel.Parameters.AddWithValue("@tableID", txtIDTable.Text);

                try
                {
                    ketNoi.Open();
                    int kq = cmdDel.ExecuteNonQuery();

                    if (kq > 0)
                    {
                        MessageBox.Show("Xóa bàn thành công");
                        LoadGridViewFoodTable();

                        // reset dữ liệu
                        txtIDTable.Text = "";
                        txtTableName.Text = "";
                        cbTableStatus.SelectedIndex = 0;
                        txtIDTable.ReadOnly = false;
                    }
                    else
                    {
                        MessageBox.Show("Không tìm thấy bàn để xóa");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi xóa bàn: " + ex.Message);
                }
                finally
                {
                    if (ketNoi.State == ConnectionState.Open)
                        ketNoi.Close();
                }
            }
        }

        private void btnEditTable_Click(object sender, EventArgs e)
        {
            if (txtIDTable.Text == "")
            {
                MessageBox.Show("Vui lòng chọn bàn cần sửa");
                return;
            }

            if (txtTableName.Text == "")
            {
                MessageBox.Show("Vui lòng nhập tên bàn");
                return;
            }

            DialogResult rs = MessageBox.Show(
                $"Bạn có chắc muốn sửa thông tin bàn [{txtTableName.Text}]?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (rs == DialogResult.No) return;

            string qUpdate = @"UPDATE FoodTable SET
                        tableName = @tableName,
                        tableStatus = @tableStatus
                        WHERE tableID = @tableID";

            SqlCommand cmdUpdate = new SqlCommand(qUpdate, ketNoi);
            cmdUpdate.Parameters.AddWithValue("@tableName", txtTableName.Text);
            cmdUpdate.Parameters.AddWithValue("@tableStatus", cbTableStatus.SelectedValue);
            cmdUpdate.Parameters.AddWithValue("@tableID", txtIDTable.Text);

            try
            {
                ketNoi.Open();
                int kq = cmdUpdate.ExecuteNonQuery();

                if (kq > 0)
                {
                    MessageBox.Show("Cập nhật bàn thành công");
                    LoadGridViewFoodTable();

                    // reset dữ liệu
                    txtIDTable.Text = "";
                    txtTableName.Text = "";
                    cbTableStatus.SelectedIndex = 0;
                    txtIDTable.ReadOnly = false;
                }
                else
                {
                    MessageBox.Show("Không tìm thấy bàn để cập nhật");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi sửa bàn: " + ex.Message);
            }
            finally
            {
                if (ketNoi.State == ConnectionState.Open)
                    ketNoi.Close();
            }
        }
        void LoadGridViewFoodDrink()
        {
            using (SqlConnection connection = new SqlConnection(connection_string_sql))
            {
                string query = @"
            SELECT 
                f.foodID,
                f.foodName,
                f.foodPrice,
                f.IDCategory,
                fc.foodCateName
            FROM Food f
            JOIN FoodCategory fc ON f.IDCategory = fc.foodCateID";

                SqlDataAdapter da = new SqlDataAdapter(query, connection);
                DataTable dtFood = new DataTable();
                da.Fill(dtFood);

                dtgvFoodDrink.DataSource = dtFood;
            }

            dtgvFoodDrink.Columns["foodID"].HeaderText = "Mã món";
            dtgvFoodDrink.Columns["foodName"].HeaderText = "Tên món";
            dtgvFoodDrink.Columns["foodPrice"].HeaderText = "Giá";
            dtgvFoodDrink.Columns["foodCateName"].HeaderText = "Danh mục";

            //  ẨN IDCategory
            dtgvFoodDrink.Columns["IDCategory"].Visible = false;
        }
        private void dtgvFoodDrink_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dtgvFoodDrink.Rows[e.RowIndex];
            txbFoodID.Text = row.Cells["foodID"].Value.ToString();
            txbFoodName.Text = row.Cells["foodName"].Value.ToString();
            txbPrice.Text = row.Cells["foodPrice"].Value.ToString();
            txbFoodID.ReadOnly = true;
            cbFoodCategory.SelectedValue = row.Cells["IDCategory"].Value;
            // LƯU GIÁ TRỊ BAN ĐẦU
            _originFoodName = txbFoodName.Text;
            _originPrice = Convert.ToDecimal(txbPrice.Text);
            _originCategoryID = cbFoodCategory.SelectedValue;
            // reset nút
            btnEditFoods.Text = "Sửa món";
        }

        private void btnFood_Click(object sender, EventArgs e)
        {
            if (txbFoodName.Text.Trim().Length <= 1)
            {
                MessageBox.Show("Tên món phải nhiều hơn 1 ký tự");
                return;
            }
            else
            {
                string qSelect = "SELECT COUNT(*) FROM Food WHERE foodName = @foodName";
                SqlCommand boLenh = new SqlCommand(qSelect, ketNoi);
                boLenh.Parameters.AddWithValue("@foodName", txbFoodName.Text.Trim());
                try
                {
                    ketNoi.Open();
                    int checkFoodName = (int)boLenh.ExecuteScalar();
                    if (checkFoodName != 0)
                    {
                        MessageBox.Show("Tên món đã tồn tại, vui lòng chọn tên khác");
                        ClearFoodInput();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi kiểm tra tên món: " + ex.Message);
                    return;
                }
                finally
                {
                    if (ketNoi.State == ConnectionState.Open)
                        ketNoi.Close();
                }
            }
            if (txbFoodName.Text == "")
            {
                MessageBox.Show("Vui lòng nhập tên món");
                return;
            }
            if (!TryGetFoodPrice(out decimal price))
            {
                return; // Đã có message trong TryGetFoodPrice
            }
            string qAdd = "INSERT INTO Food (foodName, foodPrice, IDCategory) " +
                          "VALUES (@foodName, @foodPrice, @IDCategory)";
            boDocGhi = new SqlDataAdapter(); // Tạo mới để tránh xung đột với account
            boDocGhi.InsertCommand = new SqlCommand(qAdd, ketNoi);
            boDocGhi.InsertCommand.Parameters.AddWithValue("@foodName", txbFoodName.Text.Trim());
            boDocGhi.InsertCommand.Parameters.AddWithValue("@foodPrice", price);
            boDocGhi.InsertCommand.Parameters.AddWithValue("@IDCategory", cbFoodCategory.SelectedValue);
            try
            {
                ketNoi.Open();
                boDocGhi.InsertCommand.ExecuteNonQuery();
                MessageBox.Show("Thêm món thành công");
                LoadGridViewFoodDrink();
                ClearFoodInput();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi thêm món: " + ex.Message);
                return;
            }
            finally
            {
                if (ketNoi.State == ConnectionState.Open)
                    ketNoi.Close();
            }
        }



        private void btnDelFood_Click(object sender, EventArgs e)
        {
            if (txbFoodID.Text == "")
            {
                MessageBox.Show("Vui lòng chọn món cần xóa");
                return;
            }
            DialogResult rs = MessageBox.Show(
                $"Bạn có chắc muốn xóa món [{txbFoodName.Text}]?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (rs == DialogResult.No) return;
            string query = "DELETE FROM Food WHERE foodID = @id";
            using (SqlConnection conn = new SqlConnection(connection_string_sql))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", txbFoodID.Text);
                try
                {
                    conn.Open();
                    int kq = cmd.ExecuteNonQuery();
                    if (kq > 0)
                    {
                        MessageBox.Show("Xóa món thành công");
                        LoadGridViewFoodDrink();
                        ClearFoodInput();
                        txbFoodID.ReadOnly = false; // Reset ReadOnly giống như account
                    }
                    else
                    {
                        MessageBox.Show("Không tìm thấy món để xóa");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi xóa món: " + ex.Message);
                }
            }
        }


        private void btnEditFoods_Click(object sender, EventArgs e)
        {
            if (txbFoodID.Text == "")
            {
                MessageBox.Show("Vui lòng chọn món cần sửa");
                return;
            }
            if (txbFoodName.Text == "")
            {
                MessageBox.Show("Tên món không được để trống");
                return;
            }
            if (!TryGetFoodPrice(out decimal price))
            {
                return;
            }
            DialogResult rs = MessageBox.Show(
                $"Bạn có chắc muốn sửa món [{txbFoodName.Text}]?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (rs == DialogResult.No) return;
            using (SqlConnection conn = new SqlConnection(connection_string_sql))
            {
                string query = @"UPDATE Food
                         SET foodName = @foodName,
                             foodPrice = @foodPrice,
                             IDCategory = @IDCategory
                         WHERE foodID = @foodID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@foodName", txbFoodName.Text);
                cmd.Parameters.AddWithValue("@foodPrice", price);
                cmd.Parameters.AddWithValue("@IDCategory", cbFoodCategory.SelectedValue);
                cmd.Parameters.AddWithValue("@foodID", txbFoodID.Text);
                try
                {
                    conn.Open();
                    int kq = cmd.ExecuteNonQuery();
                    if (kq > 0)
                    {
                        MessageBox.Show("Cập nhật món thành công");
                        LoadGridViewFoodDrink();
                        // reset lại trạng thái gốc
                        _originFoodName = txbFoodName.Text;
                        _originPrice = price;
                        _originCategoryID = cbFoodCategory.SelectedValue;
                        btnEditFoods.Text = "Sửa món";
                        // clear input
                        ClearFoodInput();
                    }
                    else
                    {
                        MessageBox.Show("Không tìm thấy món để cập nhật");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi sửa món: " + ex.Message);
                }
            }
        }


    }
}
