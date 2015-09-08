using System.Windows.Forms;

namespace Corecalc {
  partial class WorkbookForm {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing) {
      if (disposing && (components != null)) {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
      this.menuStrip1 = new System.Windows.Forms.MenuStrip();
      this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.importSheetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.cutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.insertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.newWorkbookToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.newFunctionSheetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.columnToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.rowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.recalculateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.recalculateFullToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.recalculateFullRebuildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.referenceFormatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.a1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.c0R0ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.r1C1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.showFormulasToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.sDFToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.auditToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.addPrecedentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.fewerPrecedentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.moreDependentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.fewerDependentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.eraseArrowsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.benchmarksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.setNumberOfBenchmarksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.numberOfRunsTextBox = new System.Windows.Forms.ToolStripTextBox();
      this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
      this.executeBenchmarksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.fullRecalculationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.fullRecalculationAfterRebuildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
      this.aboutToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
      this.statusStrip1 = new System.Windows.Forms.StatusStrip();
      this.statusLine = new System.Windows.Forms.ToolStripStatusLabel();
      this.splitContainer1 = new System.Windows.Forms.SplitContainer();
      this.formulaBox = new System.Windows.Forms.TextBox();
      this.sheetHolder = new System.Windows.Forms.TabControl();
      this.progressBar1 = new System.Windows.Forms.ProgressBar();
      this.menuStrip1.SuspendLayout();
      this.statusStrip1.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
      this.splitContainer1.Panel1.SuspendLayout();
      this.splitContainer1.Panel2.SuspendLayout();
      this.splitContainer1.SuspendLayout();
      this.SuspendLayout();
      // 
      // menuStrip1
      // 
      this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.insertToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.auditToolStripMenuItem,
            this.benchmarksToolStripMenuItem,
            this.aboutToolStripMenuItem});
      this.menuStrip1.Location = new System.Drawing.Point(0, 0);
      this.menuStrip1.Name = "menuStrip1";
      this.menuStrip1.Padding = new System.Windows.Forms.Padding(12, 4, 0, 4);
      this.menuStrip1.Size = new System.Drawing.Size(1346, 40);
      this.menuStrip1.TabIndex = 0;
      this.menuStrip1.Text = "menuStrip1";
      // 
      // fileToolStripMenuItem
      // 
      this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.importSheetToolStripMenuItem,
            this.exitToolStripMenuItem});
      this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
      this.fileToolStripMenuItem.Size = new System.Drawing.Size(58, 32);
      this.fileToolStripMenuItem.Text = "File";
      this.fileToolStripMenuItem.ToolTipText = "Add new sheet to workbook";
      // 
      // importSheetToolStripMenuItem
      // 
      this.importSheetToolStripMenuItem.Name = "importSheetToolStripMenuItem";
      this.importSheetToolStripMenuItem.ShortcutKeyDisplayString = "";
      this.importSheetToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
      this.importSheetToolStripMenuItem.Size = new System.Drawing.Size(336, 32);
      this.importSheetToolStripMenuItem.Text = "Import workbook";
      this.importSheetToolStripMenuItem.ToolTipText = "Import workbook from file and discard current workbook";
      this.importSheetToolStripMenuItem.Click += new System.EventHandler(this.importSheetToolStripMenuItem_Click);
      // 
      // exitToolStripMenuItem
      // 
      this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
      this.exitToolStripMenuItem.Size = new System.Drawing.Size(336, 32);
      this.exitToolStripMenuItem.Text = "Exit";
      this.exitToolStripMenuItem.ToolTipText = "Discard workbook and exit";
      this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
      // 
      // editToolStripMenuItem
      // 
      this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem,
            this.cutToolStripMenuItem,
            this.pasteToolStripMenuItem,
            this.deleteToolStripMenuItem});
      this.editToolStripMenuItem.Name = "editToolStripMenuItem";
      this.editToolStripMenuItem.Size = new System.Drawing.Size(63, 32);
      this.editToolStripMenuItem.Text = "Edit";
      // 
      // copyToolStripMenuItem
      // 
      this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
      this.copyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
      this.copyToolStripMenuItem.Size = new System.Drawing.Size(217, 32);
      this.copyToolStripMenuItem.Text = "Copy";
      this.copyToolStripMenuItem.ToolTipText = "Copy current cell to clipboard";
      this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
      // 
      // cutToolStripMenuItem
      // 
      this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
      this.cutToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
      this.cutToolStripMenuItem.Size = new System.Drawing.Size(217, 32);
      this.cutToolStripMenuItem.Text = "Cut";
      this.cutToolStripMenuItem.ToolTipText = "Cut current cell and move to clipboard";
      this.cutToolStripMenuItem.Click += new System.EventHandler(this.cutToolStripMenuItem_Click);
      // 
      // pasteToolStripMenuItem
      // 
      this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
      this.pasteToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
      this.pasteToolStripMenuItem.Size = new System.Drawing.Size(217, 32);
      this.pasteToolStripMenuItem.Text = "Paste";
      this.pasteToolStripMenuItem.ToolTipText = "Paste from clipboard";
      this.pasteToolStripMenuItem.Click += new System.EventHandler(this.pasteToolStripMenuItem_Click);
      // 
      // deleteToolStripMenuItem
      // 
      this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
      this.deleteToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
      this.deleteToolStripMenuItem.Size = new System.Drawing.Size(217, 32);
      this.deleteToolStripMenuItem.Text = "Delete";
      this.deleteToolStripMenuItem.ToolTipText = "Delete current cell contents";
      this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
      // 
      // insertToolStripMenuItem
      // 
      this.insertToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newWorkbookToolStripMenuItem,
            this.newFunctionSheetToolStripMenuItem,
            this.columnToolStripMenuItem,
            this.rowToolStripMenuItem});
      this.insertToolStripMenuItem.Name = "insertToolStripMenuItem";
      this.insertToolStripMenuItem.Size = new System.Drawing.Size(84, 32);
      this.insertToolStripMenuItem.Text = "Insert";
      // 
      // newWorkbookToolStripMenuItem
      // 
      this.newWorkbookToolStripMenuItem.Name = "newWorkbookToolStripMenuItem";
      this.newWorkbookToolStripMenuItem.ShortcutKeyDisplayString = "";
      this.newWorkbookToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
      this.newWorkbookToolStripMenuItem.Size = new System.Drawing.Size(361, 32);
      this.newWorkbookToolStripMenuItem.Text = "New sheet";
      this.newWorkbookToolStripMenuItem.Click += new System.EventHandler(this.newWorkbookToolStripMenuItem_Click);
      // 
      // newFunctionSheetToolStripMenuItem
      // 
      this.newFunctionSheetToolStripMenuItem.Name = "newFunctionSheetToolStripMenuItem";
      this.newFunctionSheetToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.M)));
      this.newFunctionSheetToolStripMenuItem.Size = new System.Drawing.Size(361, 32);
      this.newFunctionSheetToolStripMenuItem.Text = "New function sheet";
      this.newFunctionSheetToolStripMenuItem.Click += new System.EventHandler(this.newFunctionSheetMenuItem_Click);
      // 
      // columnToolStripMenuItem
      // 
      this.columnToolStripMenuItem.Name = "columnToolStripMenuItem";
      this.columnToolStripMenuItem.Size = new System.Drawing.Size(361, 32);
      this.columnToolStripMenuItem.Text = "Column";
      this.columnToolStripMenuItem.ToolTipText = "Insert new column to the left of current column";
      this.columnToolStripMenuItem.Click += new System.EventHandler(this.columnToolStripMenuItem_Click);
      // 
      // rowToolStripMenuItem
      // 
      this.rowToolStripMenuItem.Name = "rowToolStripMenuItem";
      this.rowToolStripMenuItem.Size = new System.Drawing.Size(361, 32);
      this.rowToolStripMenuItem.Text = "Row";
      this.rowToolStripMenuItem.ToolTipText = "Insert new row before current row";
      this.rowToolStripMenuItem.Click += new System.EventHandler(this.rowToolStripMenuItem_Click);
      // 
      // toolsToolStripMenuItem
      // 
      this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.recalculateToolStripMenuItem,
            this.recalculateFullToolStripMenuItem,
            this.recalculateFullRebuildToolStripMenuItem,
            this.referenceFormatToolStripMenuItem,
            this.showFormulasToolStripMenuItem,
            this.sDFToolStripMenuItem});
      this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
      this.toolsToolStripMenuItem.Size = new System.Drawing.Size(76, 32);
      this.toolsToolStripMenuItem.Text = "Tools";
      // 
      // recalculateToolStripMenuItem
      // 
      this.recalculateToolStripMenuItem.Name = "recalculateToolStripMenuItem";
      this.recalculateToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F9;
      this.recalculateToolStripMenuItem.Size = new System.Drawing.Size(508, 32);
      this.recalculateToolStripMenuItem.Text = "Recalculate";
      this.recalculateToolStripMenuItem.ToolTipText = "Recalculate workbook";
      this.recalculateToolStripMenuItem.Click += new System.EventHandler(this.recalculateMenuItem_Click);
      // 
      // recalculateFullToolStripMenuItem
      // 
      this.recalculateFullToolStripMenuItem.Name = "recalculateFullToolStripMenuItem";
      this.recalculateFullToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt) 
            | System.Windows.Forms.Keys.F9)));
      this.recalculateFullToolStripMenuItem.Size = new System.Drawing.Size(508, 32);
      this.recalculateFullToolStripMenuItem.Text = "Recalculate full";
      this.recalculateFullToolStripMenuItem.Click += new System.EventHandler(this.recalculateFullMenuItem_Click);
      // 
      // recalculateFullRebuildToolStripMenuItem
      // 
      this.recalculateFullRebuildToolStripMenuItem.Name = "recalculateFullRebuildToolStripMenuItem";
      this.recalculateFullRebuildToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt) 
            | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.F9)));
      this.recalculateFullRebuildToolStripMenuItem.Size = new System.Drawing.Size(508, 32);
      this.recalculateFullRebuildToolStripMenuItem.Text = "Recalculate full rebuild";
      this.recalculateFullRebuildToolStripMenuItem.Click += new System.EventHandler(this.recalculateFullRebuildMenuItem_Click);
      // 
      // referenceFormatToolStripMenuItem
      // 
      this.referenceFormatToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.a1ToolStripMenuItem,
            this.c0R0ToolStripMenuItem,
            this.r1C1ToolStripMenuItem});
      this.referenceFormatToolStripMenuItem.Name = "referenceFormatToolStripMenuItem";
      this.referenceFormatToolStripMenuItem.Size = new System.Drawing.Size(508, 32);
      this.referenceFormatToolStripMenuItem.Text = "Reference format";
      // 
      // a1ToolStripMenuItem
      // 
      this.a1ToolStripMenuItem.Name = "a1ToolStripMenuItem";
      this.a1ToolStripMenuItem.Size = new System.Drawing.Size(138, 32);
      this.a1ToolStripMenuItem.Text = "A1";
      this.a1ToolStripMenuItem.ToolTipText = "Show references in A1 format";
      this.a1ToolStripMenuItem.Click += new System.EventHandler(this.a1ToolStripMenuItem_Click);
      // 
      // c0R0ToolStripMenuItem
      // 
      this.c0R0ToolStripMenuItem.Name = "c0R0ToolStripMenuItem";
      this.c0R0ToolStripMenuItem.Size = new System.Drawing.Size(138, 32);
      this.c0R0ToolStripMenuItem.Text = "C0R0";
      this.c0R0ToolStripMenuItem.ToolTipText = "Show references in C0R0 format";
      this.c0R0ToolStripMenuItem.Click += new System.EventHandler(this.c0R0ToolStripMenuItem_Click);
      // 
      // r1C1ToolStripMenuItem
      // 
      this.r1C1ToolStripMenuItem.CheckOnClick = true;
      this.r1C1ToolStripMenuItem.Name = "r1C1ToolStripMenuItem";
      this.r1C1ToolStripMenuItem.Size = new System.Drawing.Size(138, 32);
      this.r1C1ToolStripMenuItem.Text = "R1C1";
      this.r1C1ToolStripMenuItem.ToolTipText = "Show references in R1C1 format";
      this.r1C1ToolStripMenuItem.Click += new System.EventHandler(this.r1C1ToolStripMenuItem_Click);
      // 
      // showFormulasToolStripMenuItem
      // 
      this.showFormulasToolStripMenuItem.CheckOnClick = true;
      this.showFormulasToolStripMenuItem.Name = "showFormulasToolStripMenuItem";
      this.showFormulasToolStripMenuItem.Size = new System.Drawing.Size(508, 32);
      this.showFormulasToolStripMenuItem.Text = "Show formulas";
      this.showFormulasToolStripMenuItem.Click += new System.EventHandler(this.showFormulasMenuItem_Click);
      // 
      // sDFToolStripMenuItem
      // 
      this.sDFToolStripMenuItem.Name = "sDFToolStripMenuItem";
      this.sDFToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
      this.sDFToolStripMenuItem.Size = new System.Drawing.Size(508, 32);
      this.sDFToolStripMenuItem.Text = "SDF";
      this.sDFToolStripMenuItem.Click += new System.EventHandler(this.sdfMenuItem_Click);
      // 
      // auditToolStripMenuItem
      // 
      this.auditToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addPrecedentsToolStripMenuItem,
            this.fewerPrecedentsToolStripMenuItem,
            this.moreDependentsToolStripMenuItem,
            this.fewerDependentsToolStripMenuItem,
            this.eraseArrowsToolStripMenuItem});
      this.auditToolStripMenuItem.Name = "auditToolStripMenuItem";
      this.auditToolStripMenuItem.Size = new System.Drawing.Size(77, 32);
      this.auditToolStripMenuItem.Text = "Audit";
      // 
      // addPrecedentsToolStripMenuItem
      // 
      this.addPrecedentsToolStripMenuItem.Name = "addPrecedentsToolStripMenuItem";
      this.addPrecedentsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.P)));
      this.addPrecedentsToolStripMenuItem.Size = new System.Drawing.Size(414, 32);
      this.addPrecedentsToolStripMenuItem.Text = "More precedents";
      this.addPrecedentsToolStripMenuItem.Click += new System.EventHandler(this.morePrecedentsToolStripMenuItem_Click);
      // 
      // fewerPrecedentsToolStripMenuItem
      // 
      this.fewerPrecedentsToolStripMenuItem.Name = "fewerPrecedentsToolStripMenuItem";
      this.fewerPrecedentsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.P)));
      this.fewerPrecedentsToolStripMenuItem.Size = new System.Drawing.Size(414, 32);
      this.fewerPrecedentsToolStripMenuItem.Text = "Fewer precedents";
      this.fewerPrecedentsToolStripMenuItem.Click += new System.EventHandler(this.fewerPrecedentsToolStripMenuItem_Click);
      // 
      // moreDependentsToolStripMenuItem
      // 
      this.moreDependentsToolStripMenuItem.Name = "moreDependentsToolStripMenuItem";
      this.moreDependentsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
      this.moreDependentsToolStripMenuItem.Size = new System.Drawing.Size(414, 32);
      this.moreDependentsToolStripMenuItem.Text = "More dependents";
      this.moreDependentsToolStripMenuItem.Click += new System.EventHandler(this.moreDependentsToolStripMenuItem_Click);
      // 
      // fewerDependentsToolStripMenuItem
      // 
      this.fewerDependentsToolStripMenuItem.Name = "fewerDependentsToolStripMenuItem";
      this.fewerDependentsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.D)));
      this.fewerDependentsToolStripMenuItem.Size = new System.Drawing.Size(414, 32);
      this.fewerDependentsToolStripMenuItem.Text = "Fewer dependents";
      this.fewerDependentsToolStripMenuItem.Click += new System.EventHandler(this.fewerDependentsToolStripMenuItem_Click);
      // 
      // eraseArrowsToolStripMenuItem
      // 
      this.eraseArrowsToolStripMenuItem.Name = "eraseArrowsToolStripMenuItem";
      this.eraseArrowsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
      this.eraseArrowsToolStripMenuItem.Size = new System.Drawing.Size(414, 32);
      this.eraseArrowsToolStripMenuItem.Text = "Erase arrows";
      this.eraseArrowsToolStripMenuItem.Click += new System.EventHandler(this.eraseArrowsToolStripMenuItem_Click);
      // 
      // benchmarksToolStripMenuItem
      // 
      this.benchmarksToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.setNumberOfBenchmarksToolStripMenuItem,
            this.numberOfRunsTextBox,
            this.toolStripSeparator1,
            this.executeBenchmarksToolStripMenuItem,
            this.fullRecalculationToolStripMenuItem,
            this.fullRecalculationAfterRebuildToolStripMenuItem});
      this.benchmarksToolStripMenuItem.Name = "benchmarksToolStripMenuItem";
      this.benchmarksToolStripMenuItem.Size = new System.Drawing.Size(147, 32);
      this.benchmarksToolStripMenuItem.Text = "Benchmarks";
      // 
      // setNumberOfBenchmarksToolStripMenuItem
      // 
      this.setNumberOfBenchmarksToolStripMenuItem.Name = "setNumberOfBenchmarksToolStripMenuItem";
      this.setNumberOfBenchmarksToolStripMenuItem.Size = new System.Drawing.Size(383, 32);
      this.setNumberOfBenchmarksToolStripMenuItem.Text = "Number of benchmark runs:";
      // 
      // numberOfRunsTextBox
      // 
      this.numberOfRunsTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      this.numberOfRunsTextBox.Name = "numberOfRunsTextBox";
      this.numberOfRunsTextBox.Size = new System.Drawing.Size(100, 34);
      this.numberOfRunsTextBox.Text = "10";
      // 
      // toolStripSeparator1
      // 
      this.toolStripSeparator1.Name = "toolStripSeparator1";
      this.toolStripSeparator1.Size = new System.Drawing.Size(380, 6);
      // 
      // executeBenchmarksToolStripMenuItem
      // 
      this.executeBenchmarksToolStripMenuItem.Name = "executeBenchmarksToolStripMenuItem";
      this.executeBenchmarksToolStripMenuItem.Size = new System.Drawing.Size(383, 32);
      this.executeBenchmarksToolStripMenuItem.Text = "Standard recalculation";
      this.executeBenchmarksToolStripMenuItem.Click += new System.EventHandler(this.benchmarkStandardRecalculation_Click);
      // 
      // fullRecalculationToolStripMenuItem
      // 
      this.fullRecalculationToolStripMenuItem.Name = "fullRecalculationToolStripMenuItem";
      this.fullRecalculationToolStripMenuItem.Size = new System.Drawing.Size(383, 32);
      this.fullRecalculationToolStripMenuItem.Text = "Full recalculation";
      this.fullRecalculationToolStripMenuItem.Click += new System.EventHandler(this.fullRecalculationMenuItem_Click);
      // 
      // fullRecalculationAfterRebuildToolStripMenuItem
      // 
      this.fullRecalculationAfterRebuildToolStripMenuItem.Name = "fullRecalculationAfterRebuildToolStripMenuItem";
      this.fullRecalculationAfterRebuildToolStripMenuItem.Size = new System.Drawing.Size(383, 32);
      this.fullRecalculationAfterRebuildToolStripMenuItem.Text = "Full recalculation after rebuild";
      this.fullRecalculationAfterRebuildToolStripMenuItem.Click += new System.EventHandler(this.recalculationFullRebuildMenuItem_Click);
      // 
      // aboutToolStripMenuItem
      // 
      this.aboutToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem1});
      this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
      this.aboutToolStripMenuItem.Size = new System.Drawing.Size(70, 32);
      this.aboutToolStripMenuItem.Text = "Help";
      // 
      // aboutToolStripMenuItem1
      // 
      this.aboutToolStripMenuItem1.Name = "aboutToolStripMenuItem1";
      this.aboutToolStripMenuItem1.Size = new System.Drawing.Size(144, 32);
      this.aboutToolStripMenuItem1.Text = "About";
      this.aboutToolStripMenuItem1.Click += new System.EventHandler(this.aboutToolStripMenuItem1_Click);
      // 
      // statusStrip1
      // 
      this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLine});
      this.statusStrip1.Location = new System.Drawing.Point(0, 515);
      this.statusStrip1.Name = "statusStrip1";
      this.statusStrip1.Padding = new System.Windows.Forms.Padding(2, 0, 28, 0);
      this.statusStrip1.Size = new System.Drawing.Size(1346, 29);
      this.statusStrip1.TabIndex = 1;
      this.statusStrip1.Text = "statusStrip1";
      // 
      // statusLine
      // 
      this.statusLine.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.statusLine.Name = "statusLine";
      this.statusLine.Size = new System.Drawing.Size(62, 24);
      this.statusLine.Text = "XXXX";
      // 
      // splitContainer1
      // 
      this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
      this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
      this.splitContainer1.Location = new System.Drawing.Point(0, 40);
      this.splitContainer1.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
      this.splitContainer1.Name = "splitContainer1";
      this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
      // 
      // splitContainer1.Panel1
      // 
      this.splitContainer1.Panel1.Controls.Add(this.formulaBox);
      // 
      // splitContainer1.Panel2
      // 
      this.splitContainer1.Panel2.Controls.Add(this.sheetHolder);
      this.splitContainer1.Size = new System.Drawing.Size(1346, 475);
      this.splitContainer1.SplitterDistance = 25;
      this.splitContainer1.SplitterWidth = 8;
      this.splitContainer1.TabIndex = 2;
      // 
      // formulaBox
      // 
      this.formulaBox.Dock = System.Windows.Forms.DockStyle.Fill;
      this.formulaBox.Location = new System.Drawing.Point(0, 0);
      this.formulaBox.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
      this.formulaBox.Name = "formulaBox";
      this.formulaBox.Size = new System.Drawing.Size(1346, 31);
      this.formulaBox.TabIndex = 0;
      this.formulaBox.TextChanged += new System.EventHandler(this.formulaBox_TextChanged);
      this.formulaBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.formulaBox_KeyPress);
      // 
      // sheetHolder
      // 
      this.sheetHolder.Alignment = System.Windows.Forms.TabAlignment.Bottom;
      this.sheetHolder.Dock = System.Windows.Forms.DockStyle.Fill;
      this.sheetHolder.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
      this.sheetHolder.Location = new System.Drawing.Point(0, 0);
      this.sheetHolder.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
      this.sheetHolder.Name = "sheetHolder";
      this.sheetHolder.SelectedIndex = 0;
      this.sheetHolder.Size = new System.Drawing.Size(1346, 442);
      this.sheetHolder.TabIndex = 0;
      this.sheetHolder.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.sheetHolder_DrawItem);
      // 
      // progressBar1
      // 
      this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.progressBar1.Location = new System.Drawing.Point(438, 513);
      this.progressBar1.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
      this.progressBar1.Name = "progressBar1";
      this.progressBar1.Size = new System.Drawing.Size(852, 31);
      this.progressBar1.TabIndex = 3;
      this.progressBar1.Visible = false;
      // 
      // WorkbookForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(1346, 544);
      this.Controls.Add(this.progressBar1);
      this.Controls.Add(this.splitContainer1);
      this.Controls.Add(this.statusStrip1);
      this.Controls.Add(this.menuStrip1);
      this.MainMenuStrip = this.menuStrip1;
      this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
      this.Name = "WorkbookForm";
      this.Text = "Funcalc 2014";
      this.menuStrip1.ResumeLayout(false);
      this.menuStrip1.PerformLayout();
      this.statusStrip1.ResumeLayout(false);
      this.statusStrip1.PerformLayout();
      this.splitContainer1.Panel1.ResumeLayout(false);
      this.splitContainer1.Panel1.PerformLayout();
      this.splitContainer1.Panel2.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
      this.splitContainer1.ResumeLayout(false);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.MenuStrip menuStrip1;
      private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem importSheetToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem insertToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem columnToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem rowToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
    private System.Windows.Forms.StatusStrip statusStrip1;
    private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem cutToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem recalculateToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem referenceFormatToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem a1ToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem c0R0ToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem r1C1ToolStripMenuItem;
    private System.Windows.Forms.ToolStripStatusLabel statusLine;
    private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem1;
    private System.Windows.Forms.SplitContainer splitContainer1;
    private System.Windows.Forms.TabControl sheetHolder;
    private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
    public System.Windows.Forms.TextBox formulaBox;
      private ToolStripMenuItem newWorkbookToolStripMenuItem;
      private ToolStripMenuItem newFunctionSheetToolStripMenuItem;
      private ProgressBar progressBar1;
      private ToolStripMenuItem sDFToolStripMenuItem;
      private ToolStripMenuItem benchmarksToolStripMenuItem;
      private ToolStripMenuItem setNumberOfBenchmarksToolStripMenuItem;
      private ToolStripTextBox numberOfRunsTextBox;
      private ToolStripSeparator toolStripSeparator1;
      private ToolStripMenuItem executeBenchmarksToolStripMenuItem;
      private ToolStripMenuItem showFormulasToolStripMenuItem;
      private ToolStripMenuItem recalculateFullToolStripMenuItem;
      private ToolStripMenuItem recalculateFullRebuildToolStripMenuItem;
      private ToolStripMenuItem fullRecalculationToolStripMenuItem;
      private ToolStripMenuItem fullRecalculationAfterRebuildToolStripMenuItem;
      private ToolStripMenuItem auditToolStripMenuItem;
      private ToolStripMenuItem addPrecedentsToolStripMenuItem;
      private ToolStripMenuItem fewerPrecedentsToolStripMenuItem;
      private ToolStripMenuItem moreDependentsToolStripMenuItem;
      private ToolStripMenuItem fewerDependentsToolStripMenuItem;
      private ToolStripMenuItem eraseArrowsToolStripMenuItem;
  }
}