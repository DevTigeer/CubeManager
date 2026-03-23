using System.Drawing;
using CubeManager.Helpers;

namespace CubeManager.Forms;

public class DocumentTab : UserControl
{
    private readonly TreeView _treeView;
    private readonly RichTextBox _richViewer;
    private readonly TextBox _txtEditor;
    private readonly string _docsRoot;
    private string? _currentFile;
    private bool _isEditMode;

    public DocumentTab()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorPalette.Surface;
        Padding = new Padding(10);

        _docsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CubeManager", "documents");
        Directory.CreateDirectory(_docsRoot);

        // Top bar
        var topBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(0, 5, 0, 5) };
        topBar.Controls.Add(new Label
        {
            Text = "업무자료", Size = new Size(100, 32),
            Font = new Font("맑은 고딕", 16f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft
        });

        var btnNew = ButtonFactory.CreatePrimary("+ 새 문서", 90);
        btnNew.Margin = new Padding(10, 0, 0, 0);
        btnNew.Click += BtnNew_Click;

        var btnEdit = ButtonFactory.CreateSecondary("편집", 60);
        btnEdit.Margin = new Padding(10, 0, 0, 0);
        btnEdit.Click += BtnEdit_Click;

        var btnSave = ButtonFactory.CreateSuccess("저장", 60);
        btnSave.Margin = new Padding(5, 0, 0, 0);
        btnSave.Click += BtnSave_Click;

        var btnDelete = ButtonFactory.CreateDanger("삭제", 60);
        btnDelete.Margin = new Padding(5, 0, 0, 0);
        btnDelete.Click += BtnDelete_Click;

        topBar.Controls.AddRange([btnNew, btnEdit, btnSave, btnDelete]);

        // Split: Tree + Content
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 200,
            Orientation = Orientation.Vertical
        };

        _treeView = new TreeView
        {
            Dock = DockStyle.Fill,
            Font = new Font("맑은 고딕", 10f),
            ShowLines = true
        };
        _treeView.AfterSelect += TreeView_AfterSelect;

        _richViewer = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new Font("맑은 고딕", 11f),
            BackColor = ColorPalette.Surface,
            BorderStyle = BorderStyle.None
        };

        _txtEditor = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            Font = new Font("Consolas", 11f),
            Visible = false
        };
        // Ctrl+S 단축키
        _txtEditor.KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                e.SuppressKeyPress = true;
                BtnSave_Click(null, EventArgs.Empty);
            }
        };

        split.Panel1.Controls.Add(_treeView);
        split.Panel2.Controls.Add(_richViewer);
        split.Panel2.Controls.Add(_txtEditor);

        Controls.Add(split);
        Controls.Add(topBar);

        LoadTree();
    }

    private void LoadTree()
    {
        _treeView.Nodes.Clear();
        var root = new TreeNode("문서") { Tag = _docsRoot };
        LoadDirectory(root, _docsRoot);
        _treeView.Nodes.Add(root);
        root.Expand();
    }

    private static void LoadDirectory(TreeNode parentNode, string path)
    {
        foreach (var dir in Directory.GetDirectories(path))
        {
            var node = new TreeNode(Path.GetFileName(dir)) { Tag = dir };
            LoadDirectory(node, dir);
            parentNode.Nodes.Add(node);
        }
        foreach (var file in Directory.GetFiles(path, "*.md"))
        {
            parentNode.Nodes.Add(new TreeNode(Path.GetFileNameWithoutExtension(file)) { Tag = file });
        }
    }

    private void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        var path = e.Node?.Tag as string;
        if (path == null || !File.Exists(path)) return;

        // 편집 중 파일 전환 시 저장 확인
        if (_isEditMode && _currentFile != null)
        {
            var result = MessageBox.Show("편집 중인 내용이 있습니다. 저장하시겠습니까?",
                "저장 확인", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes) BtnSave_Click(null, EventArgs.Empty);
            else if (result == DialogResult.Cancel) return;
            _isEditMode = false;
            _richViewer.Visible = true;
            _txtEditor.Visible = false;
        }

        _currentFile = path;
        var content = File.ReadAllText(path);
        _richViewer.Text = content;
        _txtEditor.Text = content;
        _isEditMode = false;
        _richViewer.Visible = true;
        _txtEditor.Visible = false;
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (_currentFile == null) return;
        _isEditMode = !_isEditMode;
        _richViewer.Visible = !_isEditMode;
        _txtEditor.Visible = _isEditMode;
        if (_isEditMode) _txtEditor.Text = File.ReadAllText(_currentFile);
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (_currentFile == null || !_isEditMode) return;
        File.WriteAllText(_currentFile, _txtEditor.Text);
        _richViewer.Text = _txtEditor.Text;
        _isEditMode = false;
        _richViewer.Visible = true;
        _txtEditor.Visible = false;
        ToastNotification.Show("저장되었습니다.", ToastType.Success);
    }

    private void BtnNew_Click(object? sender, EventArgs e)
    {
        var name = InputDialog.Show("파일명 (확장자 제외):", "새 문서");
        if (string.IsNullOrWhiteSpace(name)) return;

        var path = Path.Combine(_docsRoot, $"{name.Trim()}.md");
        File.WriteAllText(path, $"# {name.Trim()}\n\n");
        LoadTree();
        ToastNotification.Show("문서가 생성되었습니다.", ToastType.Success);
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (_currentFile == null || !File.Exists(_currentFile)) return;
        var fileName = Path.GetFileNameWithoutExtension(_currentFile);
        if (MessageBox.Show($"'{fileName}' 문서를 삭제하시겠습니까?\n(휴지통으로 이동됩니다)", "삭제 확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        // 휴지통으로 이동
        var trashDir = Path.Combine(_docsRoot, ".trash");
        Directory.CreateDirectory(trashDir);
        var trashName = $"{Path.GetFileNameWithoutExtension(_currentFile)}_{DateTime.Now:yyyyMMddHHmmss}.md";
        File.Move(_currentFile, Path.Combine(trashDir, trashName));

        _currentFile = null;
        _richViewer.Text = "";
        LoadTree();
        ToastNotification.Show("문서가 삭제되었습니다.", ToastType.Success);
    }
}
