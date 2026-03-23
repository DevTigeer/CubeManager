using System.Drawing;
using System.Drawing.Drawing2D;
using CubeManager.Controls;
using CubeManager.Helpers;

namespace CubeManager.Forms;

/// <summary>
/// 업무자료 탭 — 2025 리디자인.
/// 좌: 디렉토리 트리 | 우: 문서 뷰어 (파일 정보 + 내용 + 편집 모드)
/// </summary>
public class DocumentTab : UserControl
{
    private readonly TreeView _treeView;
    private readonly RichTextBox _richViewer;
    private readonly TextBox _txtEditor;
    private readonly string _docsRoot;
    private string? _currentFile;
    private bool _isEditMode;

    // 우측 상단 파일 정보
    private readonly Panel _fileInfoPanel;
    private readonly Label _lblFileName;
    private readonly Label _lblFileMeta;
    private readonly Label _lblEditMode;
    private readonly Panel _emptyState;

    // 버튼
    private readonly Button _btnEdit;
    private readonly Button _btnSave;

    public DocumentTab()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorPalette.Background;
        Padding = new Padding(10);

        _docsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CubeManager", "documents");
        Directory.CreateDirectory(_docsRoot);

        // ═══ 상단 툴바 ═══
        var topBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 45,
            WrapContents = false,
            Padding = new Padding(0, 5, 0, 5)
        };

        topBar.Controls.Add(new Label
        {
            Text = "업무자료", Size = new Size(100, 32),
            Font = new Font("맑은 고딕", 16f, FontStyle.Bold),
            ForeColor = ColorPalette.Text,
            TextAlign = ContentAlignment.MiddleLeft
        });

        var btnNew = ButtonFactory.CreatePrimary("+ 새 문서", 90);
        btnNew.Margin = new Padding(10, 0, 0, 0);
        btnNew.Click += BtnNew_Click;

        _btnEdit = ButtonFactory.CreateSecondary("편집", 60);
        _btnEdit.Margin = new Padding(10, 0, 0, 0);
        _btnEdit.Click += BtnEdit_Click;

        _btnSave = ButtonFactory.CreateSuccess("저장", 60);
        _btnSave.Margin = new Padding(5, 0, 0, 0);
        _btnSave.Click += BtnSave_Click;
        _btnSave.Enabled = false;

        var btnDelete = ButtonFactory.CreateDanger("삭제", 60);
        btnDelete.Margin = new Padding(5, 0, 0, 0);
        btnDelete.Click += BtnDelete_Click;

        topBar.Controls.AddRange([btnNew, _btnEdit, _btnSave, btnDelete]);

        // ═══ Split: 좌측 트리 | 우측 뷰어 ═══
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 200,
            SplitterWidth = 4,
            BackColor = ColorPalette.Border
        };
        split.Panel1.BackColor = ColorPalette.Surface;
        split.Panel2.BackColor = ColorPalette.Surface;

        // ── 좌측: 트리 ──
        _treeView = new TreeView
        {
            Dock = DockStyle.Fill,
            Font = new Font("맑은 고딕", 10f),
            ShowLines = true,
            BorderStyle = BorderStyle.None,
            BackColor = ColorPalette.Surface,
            ForeColor = ColorPalette.Text,
            ItemHeight = 28,
            Indent = 20
        };
        _treeView.AfterSelect += TreeView_AfterSelect;
        split.Panel1.Controls.Add(_treeView);

        // ── 우측: 뷰어 ──

        // 파일 정보 패널 (상단)
        _fileInfoPanel = new Panel
        {
            Dock = DockStyle.Top, Height = 60,
            BackColor = ColorPalette.Surface,
            Padding = new Padding(16, 10, 16, 0),
            Visible = false
        };
        _fileInfoPanel.Paint += (_, pe) =>
        {
            // 하단 구분선
            using var pen = new Pen(ColorPalette.Border);
            pe.Graphics.DrawLine(pen, 0, _fileInfoPanel.Height - 1,
                _fileInfoPanel.Width, _fileInfoPanel.Height - 1);
        };

        _lblFileName = new Label
        {
            Dock = DockStyle.Top, Height = 24,
            Font = new Font("맑은 고딕", 13f, FontStyle.Bold),
            ForeColor = ColorPalette.Text
        };

        _lblFileMeta = new Label
        {
            Dock = DockStyle.Top, Height = 20,
            Font = new Font("맑은 고딕", 9f),
            ForeColor = ColorPalette.TextTertiary
        };

        _lblEditMode = new Label
        {
            Text = "✏️ 편집 중",
            Dock = DockStyle.Right, Width = 80,
            Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
            ForeColor = ColorPalette.Warning,
            TextAlign = ContentAlignment.MiddleRight,
            Visible = false
        };

        _fileInfoPanel.Controls.Add(_lblFileMeta);
        _fileInfoPanel.Controls.Add(_lblFileName);
        _fileInfoPanel.Controls.Add(_lblEditMode);

        // 뷰어
        _richViewer = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new Font("맑은 고딕", 11f),
            BackColor = ColorPalette.Surface,
            ForeColor = ColorPalette.Text,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(16, 12, 16, 12)
        };

        // 에디터
        _txtEditor = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            Font = new Font("Consolas", 11f),
            BackColor = Color.FromArgb(252, 252, 248), // 미세한 크림색
            ForeColor = ColorPalette.Text,
            BorderStyle = BorderStyle.None,
            Visible = false
        };
        _txtEditor.KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                e.SuppressKeyPress = true;
                BtnSave_Click(null, EventArgs.Empty);
            }
        };

        // 빈 상태
        _emptyState = new Panel { Dock = DockStyle.Fill };
        _emptyState.Controls.Add(new Label
        {
            Text = "📄 문서를 선택하세요\n\n좌측 트리에서 파일을 클릭하거나\n'+ 새 문서' 버튼으로 시작하세요.",
            Font = new Font("맑은 고딕", 11f),
            ForeColor = ColorPalette.TextTertiary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        });

        split.Panel2.Controls.Add(_richViewer);
        split.Panel2.Controls.Add(_txtEditor);
        split.Panel2.Controls.Add(_emptyState);
        split.Panel2.Controls.Add(_fileInfoPanel);

        Controls.Add(split);
        Controls.Add(topBar);

        LoadTree();
    }

    private void LoadTree()
    {
        _treeView.Nodes.Clear();
        var root = new TreeNode("📁 문서") { Tag = _docsRoot };
        LoadDirectory(root, _docsRoot);
        _treeView.Nodes.Add(root);
        root.Expand();
    }

    private static void LoadDirectory(TreeNode parentNode, string path)
    {
        foreach (var dir in Directory.GetDirectories(path))
        {
            if (Path.GetFileName(dir).StartsWith('.')) continue; // .trash 숨김
            var node = new TreeNode($"📁 {Path.GetFileName(dir)}") { Tag = dir };
            LoadDirectory(node, dir);
            parentNode.Nodes.Add(node);
        }
        foreach (var file in Directory.GetFiles(path, "*.md"))
        {
            parentNode.Nodes.Add(new TreeNode($"📝 {Path.GetFileNameWithoutExtension(file)}") { Tag = file });
        }
    }

    private void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        var path = e.Node?.Tag as string;
        if (path == null || !File.Exists(path)) return;

        if (_isEditMode && _currentFile != null)
        {
            var result = MessageBox.Show("편집 중인 내용이 있습니다. 저장하시겠습니까?",
                "저장 확인", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes) BtnSave_Click(null, EventArgs.Empty);
            else if (result == DialogResult.Cancel) return;
            SetEditMode(false);
        }

        _currentFile = path;
        var content = File.ReadAllText(path);
        _richViewer.Text = content;
        _txtEditor.Text = content;

        // 파일 정보 표시
        var fi = new FileInfo(path);
        _lblFileName.Text = Path.GetFileNameWithoutExtension(path);
        _lblFileMeta.Text = $"수정: {fi.LastWriteTime:yyyy-MM-dd HH:mm}  ·  크기: {fi.Length:N0} bytes";

        _fileInfoPanel.Visible = true;
        _emptyState.Visible = false;
        _richViewer.Visible = true;
        _txtEditor.Visible = false;
        SetEditMode(false);
    }

    private void SetEditMode(bool editing)
    {
        _isEditMode = editing;
        _richViewer.Visible = !editing;
        _txtEditor.Visible = editing;
        _lblEditMode.Visible = editing;
        _btnSave.Enabled = editing;
        _btnEdit.Text = editing ? "취소" : "편집";
        _btnEdit.ForeColor = editing ? ColorPalette.Danger : ColorPalette.Primary;
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (_currentFile == null) return;
        if (_isEditMode)
        {
            // 편집 취소
            SetEditMode(false);
            return;
        }
        _txtEditor.Text = File.ReadAllText(_currentFile);
        SetEditMode(true);
        _txtEditor.Focus();
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (_currentFile == null || !_isEditMode) return;
        File.WriteAllText(_currentFile, _txtEditor.Text);
        _richViewer.Text = _txtEditor.Text;
        SetEditMode(false);

        // 메타 업데이트
        var fi = new FileInfo(_currentFile);
        _lblFileMeta.Text = $"수정: {fi.LastWriteTime:yyyy-MM-dd HH:mm}  ·  크기: {fi.Length:N0} bytes";
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

        var trashDir = Path.Combine(_docsRoot, ".trash");
        Directory.CreateDirectory(trashDir);
        var trashName = $"{Path.GetFileNameWithoutExtension(_currentFile)}_{DateTime.Now:yyyyMMddHHmmss}.md";
        File.Move(_currentFile, Path.Combine(trashDir, trashName));

        _currentFile = null;
        _richViewer.Text = "";
        _fileInfoPanel.Visible = false;
        _emptyState.Visible = true;
        _richViewer.Visible = false;
        LoadTree();
        ToastNotification.Show("문서가 삭제되었습니다.", ToastType.Success);
    }
}
