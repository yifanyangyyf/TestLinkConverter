﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using ConvertModel;
using log4net;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using OfficeOpenXml.Style;

namespace ConvertLibrary
{
    public class ExcelHandler
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(ExcelHandler));
        private readonly List<TestCase> _sourceTestCases;
        private readonly bool _isShowStepsNo;

        [Obsolete]
        public ExcelHandler(List<TestCase> outputCases)
        {
            this._sourceTestCases = outputCases;
            _isShowStepsNo = ConfigurationSettings.AppSettings["isShowStepsNo"].Equals("true");
        }

        /// <summary>
        /// 写Excel
        /// </summary>
        public void WriteExcel(string fileName)
        {
            using (var p = new ExcelPackage())
            {
                this.WriteInWorkSheet(p.Workbook);
                p.SaveAs(new FileInfo(fileName));
            }
            OutputDisplay.ShowMessage($"文件保存路勁：{fileName}\n", Color.Azure);
        }

        private void WriteInWorkSheet(ExcelWorkbook workBook)
        {
            var workSheet = workBook.Worksheets.Add("MySheet");

            int maxHierarchy = 0;
            foreach (TestCase testCase in this._sourceTestCases)
            {
                if (maxHierarchy < testCase.TestCaseHierarchy.Count)
                {
                    maxHierarchy = testCase.TestCaseHierarchy.Count;
                }
            }

            BuildTemplate(workSheet, maxHierarchy);

            int iFlag = 2;
            foreach (TestCase node in this._sourceTestCases)
            {
                if (node.Name == null && node.TestSteps == null)
                {
                    continue;
                }
                // 写Excel时进度条展示
                // ProgressBarShow.ShowProgressValue(this._sourceTestCases.IndexOf(node) * 100 / this._sourceTestCases.Count);
                workSheet.Row(iFlag).CustomHeight = true;
                workSheet.Cells[iFlag, 1].Value = node.ExternalId;

                for (int i = 0; i < node.TestCaseHierarchy.Count; i++)
                {
                    workSheet.Cells[iFlag, i + 2].Value = node.TestCaseHierarchy.ToArray()[i];
                }

                workSheet.Cells[iFlag, maxHierarchy + 2].Value = CommonHelper.DelTags(node.Name);
                string keywords = string.Empty;
                if (node.Keywords != null)
                {
                    foreach (string keyword in node.Keywords)
                    {
                        keywords = keywords + keyword + Environment.NewLine;
                    }
                }
                workSheet.Cells[iFlag, maxHierarchy + 3].Value = keywords.TrimEnd((char[])"\n\r".ToCharArray());
                workSheet.Cells[iFlag, maxHierarchy + 4].Value = node.Importance.ToString();
                workSheet.Cells[iFlag, maxHierarchy + 5].Value = node.ExecutionType.ToString();

                if (this._isShowStepsNo)
                {
                    workSheet.Cells[iFlag, maxHierarchy + 6].Value = CommonHelper.GenerateNoByLineBreak(node.Summary);
                    workSheet.Cells[iFlag, maxHierarchy + 7].Value =CommonHelper.GenerateNoByLineBreak(node.Preconditions);
                }
                else
                {
                    workSheet.Cells[iFlag, maxHierarchy + 6].Value = CommonHelper.DelTags(node.Summary);
                    workSheet.Cells[iFlag, maxHierarchy + 7].Value = CommonHelper.DelTags(node.Preconditions);
                }

                int iMerge = 0;

                if (node.TestSteps != null && node.TestSteps.Count != 0)
                {
                    foreach (var step in node.TestSteps)
                    {
                        string stepNo = string.Empty;
                        if (this._isShowStepsNo)
                        {
                            stepNo = $"{node.TestSteps.IndexOf(step) + 1}、";
                        }

                        workSheet.Cells[iFlag, maxHierarchy + 8].Value = $"{stepNo}{CommonHelper.DelTags(step.Actions)}";
                        workSheet.Cells[iFlag, maxHierarchy + 9].Value = $"{stepNo}{CommonHelper.DelTags(step.ExpectedResults)}";

                        iFlag++;
                        iMerge++;
                    }
                }
                else
                {
                    iFlag++;
                    iMerge++;
                }
                MergeCells(workSheet, iMerge, iFlag - iMerge, maxHierarchy+9);
                Thread.Sleep(50);
            }
            workSheet.Cells[iFlag, 1].Value = "END";
            ProgressBarShow.ShowProgressValue(100);

        }
        private void BuildTemplate(ExcelWorksheet ws, int maxHierarchy)
        {
            ws.Cells[1, 1].Value = "用例编号";
            // 水平居中
            ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            // 垂直居中
            ws.Cells[1, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            // 设置列宽为10mm
            ws.Column(1).Width = 10;
            for (int i = 1; i <= maxHierarchy; i++)
            {
                ws.Cells[1, i + 1].Value = NumberToChinese(i.ToString()) + "级模块";
                ws.Cells[1, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[1, i + 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ws.Column(i + 1).Width = 10;
            }

            ws.Cells[1, maxHierarchy + 2].Value = "用例名称";
            ws.Cells[1, maxHierarchy + 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[1, maxHierarchy + 2].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Column(maxHierarchy + 2).Width = 30;
            ws.Cells[1, maxHierarchy + 3].Value = "关键字";
            ws.Cells[1, maxHierarchy + 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[1, maxHierarchy + 3].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Column(maxHierarchy + 3).Width = 8;
            ws.Cells[1, maxHierarchy + 4].Value = "用例等级";
            ws.Cells[1, maxHierarchy + 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[1, maxHierarchy + 4].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Column(maxHierarchy + 4).Width = 10;
            ws.Cells[1, maxHierarchy + 5].Value = "执行方式";
            ws.Cells[1, maxHierarchy + 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[1, maxHierarchy + 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Column(maxHierarchy + 5).Width = 10;
            ws.Cells[1, maxHierarchy + 6].Value = "用例摘要";
            ws.Cells[1, maxHierarchy + 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[1, maxHierarchy + 6].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Column(maxHierarchy + 6).Width = 30;
            ws.Cells[1, maxHierarchy + 7].Value = "预置条件";
            ws.Cells[1, maxHierarchy + 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[1, maxHierarchy + 7].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Column(maxHierarchy + 7).Width = 30;
            ws.Cells[1, maxHierarchy + 8].Value = "操作步骤";
            ws.Cells[1, maxHierarchy + 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[1, maxHierarchy + 8].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Column(maxHierarchy + 8).Width = 40;
            ws.Cells[1, maxHierarchy + 9].Value = "期望结果";
            ws.Cells[1, maxHierarchy + 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[1, maxHierarchy + 9].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Column(maxHierarchy + 9).Width = 30;
            ws.Cells[1, 1, 1, maxHierarchy + 9].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[1, 1, 1, maxHierarchy + 9].Style.Fill.BackgroundColor.SetColor(Color.CornflowerBlue);
            ws.Cells[1, 1, 1, maxHierarchy + 9].Style.Font.Color.SetColor(Color.White);
            ws.Cells[1, 1, 1, maxHierarchy + 9].Style.Font.Bold = true;

            // 单元格自适应大小。
            ws.Cells.Style.ShrinkToFit = true;
            ws.Cells.Style.WrapText = true;

        }

        /// <summary>
        /// 合并单元格
        /// </summary>
        /// <param name="workSheet">指定Sheet页</param>
        /// <param name="iMerge"></param>
        /// <param name="iFlag"></param>
        /// <param name="handleCellsCount"></param>
        private static void MergeCells(ExcelWorksheet workSheet, int iMerge, int iFlag, int handleCellsCount)
        {
            //导出Excel前handleCellsCount-2列均需要合并单元格，排除预置条件和操作步骤列。
            for (int i = 1; i <= handleCellsCount-2; i++)
            {
                workSheet.Cells[iFlag, i, iFlag + iMerge - 1, i].Merge = true;
            }
        }

        private static string NumberToChinese(string inputNum)
        {
            string[] strArr = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", };
            string[] chinese = { "", "十", "百", "千", "万", "十", "百", "千", "亿" };
            char[] tmpArr = inputNum.ToArray();
            string tmpVal = "";
            for (int i = 0; i < tmpArr.Length; i++)
            {
                tmpVal += strArr[tmpArr[i] - 48];        //ASCII编码 0为48
                tmpVal += chinese[tmpArr.Length - 1 - i];//根据对应的位数插入对应的单位
            }

            return tmpVal;
        }
    }
}