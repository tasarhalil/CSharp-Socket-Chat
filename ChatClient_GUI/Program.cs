using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.Media;

public class ChatForm : Form
{
    private TextBox txtIpAddress, txtPort, txtUsername, txtMessage;
    private Button btnConnect, btnSend;
    private RichTextBox rtbChatHistory;
    private ListBox lbUsers;
    private Label lblTypingStatus;
    private System.Windows.Forms.Timer typingTimer;

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isTyping = false;

    public ChatForm()
    {
        this.Text = "Chat Uygulaması";
        this.Size = new Size(650, 500);
        // --- DEĞİŞİKLİK: Formun minimum boyutunu belirleyelim ki çok küçülmesin ---
        this.MinimumSize = new Size(500, 400);
        // --- DEĞİŞİKLİK: Formu yeniden boyutlandırılabilir yapalım ---
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true; // Büyütme butonunu aktif edelim

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        //--- Bağlantı Paneli ---
        GroupBox connectionGroup = new GroupBox() { Text = "Bağlantı Bilgileri", Location = new Point(10, 10), Size = new Size(610, 80) };
        // --- DEĞİŞİKLİK: Üst, sol ve sağ kenarlara tuttur ---
        connectionGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        connectionGroup.Controls.Add(new Label() { Text = "IP Adresi:", Location = new Point(10, 20), AutoSize = true });
        connectionGroup.Controls.Add(new Label() { Text = "Port:", Location = new Point(120, 20), AutoSize = true });
        connectionGroup.Controls.Add(new Label() { Text = "Kullanıcı Adı:", Location = new Point(180, 20), AutoSize = true });
        txtIpAddress = new TextBox() { Text = "127.0.0.1", Location = new Point(10, 40), Width = 100 };
        txtPort = new TextBox() { Text = "5000", Location = new Point(120, 40), Width = 50 };
        txtUsername = new TextBox() { Location = new Point(180, 40), Width = 120 };
        btnConnect = new Button() { Text = "Bağlan", Location = new Point(310, 38), Size = new Size(140, 25) };
        btnConnect.Click += BtnConnect_Click;
        connectionGroup.Controls.Add(txtIpAddress);
        connectionGroup.Controls.Add(txtPort);
        connectionGroup.Controls.Add(txtUsername);
        connectionGroup.Controls.Add(btnConnect);
        this.Controls.Add(connectionGroup);

        int chatAreaY = 100;
        //--- Sohbet Geçmişi ---
        rtbChatHistory = new RichTextBox() { Location = new Point(10, chatAreaY), Size = new Size(460, 280), ReadOnly = true, Font = new Font("Arial", 10) };
        // --- DEĞİŞİKLİK: Dört kenara da tutturarak esnemesini sağla ---
        rtbChatHistory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.Controls.Add(rtbChatHistory);

        //--- Online Kullanıcılar Listesi ---
        Label lblOnlineUsers = new Label() { Text = "Online Kullanıcılar", Location = new Point(480, chatAreaY) };
        // --- DEĞİŞİKLİK: Üst ve sağ kenara tuttur ---
        lblOnlineUsers.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        this.Controls.Add(lblOnlineUsers);

        lbUsers = new ListBox() { Location = new Point(480, chatAreaY + 20), Size = new Size(140, 260) };
        // --- DEĞİŞİKLİK: Üst, alt ve sağ kenarlara tuttur ---
        lbUsers.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
        lbUsers.DoubleClick += LbUsers_DoubleClick;
        this.Controls.Add(lbUsers);

        //--- "Yazıyor..." Etiketi ---
        lblTypingStatus = new Label() { Location = new Point(10, chatAreaY + 285), Size = new Size(460, 15), ForeColor = Color.Gray, Font = new Font("Arial", 8, FontStyle.Italic) };
        // --- DEĞİŞİKLİK: Alt, sol ve sağ kenarlara tuttur ---
        lblTypingStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.Controls.Add(lblTypingStatus);

        //--- Mesaj Gönderme Paneli ---
        int messageBoxY = chatAreaY + 305;
        txtMessage = new TextBox() { Location = new Point(10, messageBoxY), Size = new Size(460, 25), Enabled = false };
        // --- DEĞİŞİKLİK: Alt, sol ve sağ kenarlara tuttur ---
        txtMessage.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtMessage.TextChanged += TxtMessage_TextChanged;

        btnSend = new Button() { Text = "Gönder", Location = new Point(480, messageBoxY - 2), Size = new Size(140, 25), Enabled = false };
        // --- DEĞİŞİKLİK: Alt ve sağ kenara tuttur ---
        btnSend.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnSend.Click += BtnSend_Click;
        this.AcceptButton = btnSend;

        this.Controls.Add(txtMessage);
        this.Controls.Add(btnSend);
        this.FormClosing += ChatForm_FormClosing;

        typingTimer = new System.Windows.Forms.Timer();
        typingTimer.Interval = 1500;
        typingTimer.Tick += TypingTimer_Tick;
    }

    // Diğer tüm metotlar (BtnConnect_Click, ReceiveMessages, ProcessServerMessage vb.) aynı kalıyor...
    private void TxtMessage_TextChanged(object sender, EventArgs e)
    {
        if (stream == null || !client.Connected) return;
        if (!isTyping)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes("TYPING:");
                stream.Write(buffer, 0, buffer.Length);
                isTyping = true;
            }
            catch { }
        }
        typingTimer.Stop();
        typingTimer.Start();
    }
    private void TypingTimer_Tick(object sender, EventArgs e)
    {
        if (stream == null || !client.Connected) return;
        try
        {
            byte[] buffer = Encoding.UTF8.GetBytes("STOPTYPING:");
            stream.Write(buffer, 0, buffer.Length);
            isTyping = false;
            typingTimer.Stop();
        }
        catch { }
    }
    private void BtnConnect_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtUsername.Text))
        {
            MessageBox.Show("Lütfen bir kullanıcı adı girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            client = new TcpClient(txtIpAddress.Text, int.Parse(txtPort.Text));
            stream = client.GetStream();
            byte[] usernameBytes = Encoding.UTF8.GetBytes(txtUsername.Text);
            stream.Write(usernameBytes, 0, usernameBytes.Length);
            btnConnect.Text = "Bağlandı";
            btnConnect.Enabled = false;
            txtIpAddress.Enabled = false;
            txtPort.Enabled = false;
            txtUsername.Enabled = false;
            txtMessage.Enabled = true;
            btnSend.Enabled = true;
            receiveThread = new Thread(ReceiveMessages);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            UpdateChatHistory(new ChatMessage { Text = "Sunucuya bağlanıldı.", IsSystem = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Sunucuya bağlanılamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    private void ReceiveMessages()
    {
        byte[] buffer = new byte[2048];
        int byteCount;
        try
        {
            while ((byteCount = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, byteCount);
                ProcessServerMessage(message);
            }
        }
        catch { UpdateChatHistory(new ChatMessage { Text = "Sunucu ile bağlantı kesildi.", IsSystem = true }); }
    }
    private void ProcessServerMessage(string message)
    {
        string[] parts = message.Split(new char[] { ':' }, 2);
        if (parts.Length < 2) return;
        string command = parts[0];
        string content = parts[1];
        switch (command)
        {
            case "USERLIST": UpdateUserList(content.Split(',')); break;
            case "JOIN": AddUserToList(content); break;
            case "LEAVE": RemoveUserFromList(content); break;
            case "TYPING": this.Invoke((MethodInvoker)delegate { lblTypingStatus.Text = $"{content} yazıyor..."; }); break;
            case "STOPTYPING": this.Invoke((MethodInvoker)delegate { if (lblTypingStatus.Text.StartsWith(content)) lblTypingStatus.Text = ""; }); break;
            case "CHAT":
                UpdateChatHistory(new ChatMessage { Text = content, IsSystem = content.Contains("[Sunucu]") || content.Contains("[YÖNETİCİ]") });
                SystemSounds.Beep.Play();
                break;
            case "PM_IN":
                UpdateChatHistory(new ChatMessage { Text = content, IsPrivate = true });
                SystemSounds.Exclamation.Play();
                break;
            case "PM_OUT": UpdateChatHistory(new ChatMessage { Text = content, IsPrivate = true }); break;
            case "KICK":
                MessageBox.Show(content, "Bağlantı Kesildi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.Invoke((MethodInvoker)delegate { this.Close(); });
                break;
            case "ERROR":
                MessageBox.Show(content, "Sunucu Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Invoke((MethodInvoker)delegate { this.Close(); });
                break;
        }
    }
    private void LbUsers_DoubleClick(object sender, EventArgs e)
    {
        if (lbUsers.SelectedItem != null)
        {
            string targetUser = lbUsers.SelectedItem.ToString();
            if (targetUser.Equals(txtUsername.Text, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Kendinize özel mesaj gönderemezsiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            txtMessage.Text = $"/w {targetUser} ";
            txtMessage.Focus();
            txtMessage.SelectionStart = txtMessage.Text.Length;
        }
    }
    private void BtnSend_Click(object sender, EventArgs e)
    {
        string message = txtMessage.Text;
        if (string.IsNullOrEmpty(message)) return;
        try
        {
            TypingTimer_Tick(null, null);
            string messageToSend = message;
            if (message.StartsWith("/w "))
            {
                string[] parts = message.Split(new char[] { ' ' }, 3);
                if (parts.Length == 3)
                {
                    messageToSend = $"PM:{parts[1]}:{parts[2]}";
                }
                else
                {
                    UpdateChatHistory(new ChatMessage { Text = "Özel mesaj formatı hatalı. Kullanım: /w kullanıcı mesaj", IsSystem = true });
                    return;
                }
            }
            byte[] buffer = Encoding.UTF8.GetBytes(messageToSend);
            stream.Write(buffer, 0, buffer.Length);
            txtMessage.Clear();
        }
        catch (Exception ex) { UpdateChatHistory(new ChatMessage { Text = "Mesaj gönderilirken hata oluştu: " + ex.Message, IsSystem = true }); }
    }
    private struct ChatMessage { public string Text; public bool IsPrivate; public bool IsSystem; }
    private void UpdateChatHistory(ChatMessage msg)
    {
        if (rtbChatHistory.InvokeRequired)
        {
            rtbChatHistory.Invoke(new Action<ChatMessage>(UpdateChatHistory), msg);
        }
        else
        {
            rtbChatHistory.SelectionStart = rtbChatHistory.TextLength;
            rtbChatHistory.SelectionLength = 0;
            if (msg.IsPrivate) rtbChatHistory.SelectionColor = Color.DeepPink;
            else if (msg.IsSystem) rtbChatHistory.SelectionColor = Color.DarkGreen;
            else rtbChatHistory.SelectionColor = Color.Black;
            rtbChatHistory.AppendText(msg.Text + Environment.NewLine);
            rtbChatHistory.SelectionColor = rtbChatHistory.ForeColor;
            rtbChatHistory.ScrollToCaret();
        }
    }
    private void UpdateUserList(string[] users)
    {
        if (lbUsers.InvokeRequired) { lbUsers.Invoke(new Action<string[]>(UpdateUserList), new object[] { users }); }
        else
        {
            lbUsers.Items.Clear();
            foreach (var user in users) lbUsers.Items.Add(user);
        }
    }
    private void AddUserToList(string user)
    {
        if (lbUsers.InvokeRequired) { lbUsers.Invoke(new Action<string>(AddUserToList), user); }
        else { lbUsers.Items.Add(user); }
    }
    private void RemoveUserFromList(string user)
    {
        if (lbUsers.InvokeRequired) { lbUsers.Invoke(new Action<string>(RemoveUserFromList), user); }
        else { lbUsers.Items.Remove(user); }
    }
    private void ChatForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (client != null) client.Close();
        if (stream != null) stream.Close();
    }
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new ChatForm());
    }
}

