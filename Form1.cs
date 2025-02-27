﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _5_2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            InitializeFontComboBox();
            InitializeFontSizeComboBox();
            InitializeFontStyleComboBox();
        }
        private bool isUndoRedo = false;
        private Stack<MemoryStream> undoStack = new Stack<MemoryStream>();
        private Stack<MemoryStream> redoStack = new Stack<MemoryStream>();
        //private Stack<string> undoStack = new Stack<string>();     
        //private Stack<string> redoStack = new Stack<string>();     
        private const int MaxHistoryCount = 10;

        private int selectionStart = 0;
        private int selectionLength = 0;


        private void InitializeFontComboBox()
        {
            foreach (FontFamily font in FontFamily.Families)
            {
                comboBoxFont.Items.Add(font.Name);
            }
            comboBoxFont.SelectedIndex = 0;
        }
        private void InitializeFontSizeComboBox()
        {
            for (int i = 8; i <= 72; i += 2)
            {
                comboBoxSize.Items.Add(i);
            }
            comboBoxSize.SelectedIndex = 2;
        }
        private void InitializeFontStyleComboBox()
        {
            comboBoxStyle.Items.Add(FontStyle.Regular.ToString());
            comboBoxStyle.Items.Add(FontStyle.Bold.ToString());
            comboBoxStyle.Items.Add(FontStyle.Italic.ToString());
            comboBoxStyle.Items.Add(FontStyle.Underline.ToString());
            comboBoxStyle.Items.Add(FontStyle.Strikeout.ToString());
            comboBoxStyle.SelectedIndex = 0;
        }
        private void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectionStart = rtbText.SelectionStart;
            selectionLength = rtbText.SelectionLength;


            if (rtbText.SelectionFont != null)
            {
                string selectedFont = comboBoxFont.SelectedItem?.ToString();
                string selectedSizeStr = comboBoxSize.SelectedItem?.ToString();
                string selectedStyleStr = comboBoxStyle.SelectedItem?.ToString();

                if (selectedFont != null && selectedSizeStr != null && selectedStyleStr != null)
                {
                    float selectedSize = float.Parse(selectedSizeStr);
                    FontStyle selectedStyle = (FontStyle)Enum.Parse(typeof(FontStyle), selectedStyleStr);

                    Font currentFont = rtbText.SelectionFont;
                    FontStyle newStyle = currentFont.Style;

                    if (comboBoxStyle.SelectedItem.ToString() == FontStyle.Bold.ToString())
                        newStyle = FontStyle.Bold;
                    else if (comboBoxStyle.SelectedItem.ToString() == FontStyle.Italic.ToString())
                        newStyle = FontStyle.Italic;
                    else if (comboBoxStyle.SelectedItem.ToString() == FontStyle.Underline.ToString())
                        newStyle = FontStyle.Underline;
                    else if (comboBoxStyle.SelectedItem.ToString() == FontStyle.Strikeout.ToString())
                        newStyle = FontStyle.Strikeout;
                    else
                        newStyle = FontStyle.Regular;

                    Font newFont = new Font(selectedFont, selectedSize, newStyle);
                    rtbText.SelectionFont = newFont;
                }
            }

            rtbText.Focus();
            rtbText.Select(selectionStart, selectionLength);
        }
        private void btnOpen_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "選擇檔案";
            openFileDialog1.Filter = "RTF格式檔案 (*.rtf)|*.rtf|文字檔案 (*.txt)|*.txt|所有檔案 (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.InitialDirectory = "C:\\";
            openFileDialog1.Multiselect = true;

            DialogResult result = openFileDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                try
                {
                    string selectedFileName = openFileDialog1.FileName;

                    string fileExtension = Path.GetExtension(selectedFileName).ToLower();

                    if (fileExtension == ".txt")
                    {
                        using (FileStream fileStream = new FileStream(selectedFileName, FileMode.Open, FileAccess.Read))
                        {
                            using (StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8))
                            {
                                rtbText.Text = streamReader.ReadToEnd();
                            }
                        }
                    }
                    else if (fileExtension == ".rtf")
                    {
                        rtbText.LoadFile(selectedFileName, RichTextBoxStreamType.RichText);
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("讀取檔案時發生錯誤: " + ex.Message, "錯誤訊息", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("使用者取消了選擇檔案操作。", "訊息", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Title = "儲存檔案";
            saveFileDialog1.Filter = "RTF格式檔案 (*.rtf)|*.rtf|文字檔案 (*.txt)|*.txt|所有檔案 (*.*)|*.*";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.InitialDirectory = "C:\\";
            DialogResult result = saveFileDialog1.ShowDialog();

            FileStream fileStream = null;
            if (result == DialogResult.OK)
            {
                try
                {
                    string saveFileName = saveFileDialog1.FileName;
                    string extension = Path.GetExtension(saveFileName);
                    using (fileStream = new FileStream(saveFileName, FileMode.Create, FileAccess.Write))
                    {
                        if (extension.ToLower() == ".txt")
                        {
                            byte[] data = Encoding.UTF8.GetBytes(rtbText.Text);
                            fileStream.Write(data, 0, data.Length);
                        }
                        else if (extension.ToLower() == ".rtf")
                        {
                            rtbText.SaveFile(fileStream, RichTextBoxStreamType.RichText);
                        }
                    }

                    MessageBox.Show("檔案儲存成功。", "訊息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("儲存檔案時發生錯誤: " + ex.Message, "錯誤訊息", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    fileStream.Close();
                }
            }
            else
            {
                MessageBox.Show("使用者取消了儲存檔案操作。", "訊息", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
            }
        }

        private void btnUndo_Click(object sender, EventArgs e)
        {
            if (undoStack.Count > 1)
            {
                isUndoRedo = true;
                redoStack.Push(undoStack.Pop());
                MemoryStream lastSavedState = undoStack.Peek();
                LoadFromMemory(lastSavedState);
                //UpdateListBox();
                isUndoRedo = false;
            }
        }
        private void btnRedo_Click(object sender, EventArgs e)
        {
            if (redoStack.Count > 0)
            {
                isUndoRedo = true;
                undoStack.Push(redoStack.Pop());
                MemoryStream lastSavedState = undoStack.Peek();
                LoadFromMemory(lastSavedState);
                //UpdateListBox();
                isUndoRedo = false;
            }
        }

        private void rtbText_TextChanged(object sender, EventArgs e)
        {

            if (isUndoRedo == false)
            {
                SaveCurrentStateToStack(); 
                redoStack.Clear();            

                if (undoStack.Count > MaxHistoryCount)
                {
                    Stack<MemoryStream> tempStack = new Stack<MemoryStream>();
                    for (int i = 0; i < MaxHistoryCount; i++)
                    {
                        tempStack.Push(undoStack.Pop());
                    }
                    undoStack.Clear(); 
                                       
                    foreach (MemoryStream item in tempStack)
                    {
                        undoStack.Push(item);
                    }
                }
                //UpdateListBox();
            }
        }


        void UpdateListBox()
        {
            listUndo.Items.Clear(); // 清空 ListBox 中的元素

            // 將堆疊中的內容逐一添加到 ListBox 中
            foreach (MemoryStream item in undoStack)
            {
                listUndo.Items.Add(item);
            }
        }
        private void SaveCurrentStateToStack()
        {
            MemoryStream memoryStream = new MemoryStream();
            rtbText.SaveFile(memoryStream, RichTextBoxStreamType.RichText);
            undoStack.Push(memoryStream);
        }

        private void LoadFromMemory(MemoryStream memoryStream)
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
            rtbText.LoadFile(memoryStream, RichTextBoxStreamType.RichText);
        }
    }
}
    

