   M S B A M L   `   `   `  ���� H  DWpfInfoPusher, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null-clr-namespace:WpfInfoPusherWpfInfoPusher  C&clr-namespace:WpfInfoPusher.ConvertersWpfInfoPusher.Converters      WpfInfoPusher.MainWindow   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     -localclr-namespace:WpfInfoPusher     <	converter&clr-namespace:WpfInfoPusher.Converters       � Icon$  ./Images/icon.ico�� � Title$$ 信息交换识别推送系统��$	��350��$	��625�� � 	Resources -   'WpfInfoPusher.Converters.ColorConverter   colorConverter0   *WpfInfoPusher.Converters.TaskTypeConverter  typeConverter��%   &	        &	          .���  � RowDefinitions �� $
��auto ��� $
��auto ��� .+�o� -    o�Name$
 menu��.@�m� -   $��首页��
 � Tag$
 Main��m� -   $��创建任务��$ Create��m� -    m�Name$ 	menuStart��$��开始任务��$ Start��m� -   $ menuStop��$��停止任务��$y�	Collapsed=�$
 Stop��m� -   $��查看日志��$	 Log��m� -   $��关于��$ About����  �� Orientation$ 
Horizontal=�$��1��$
��11,5q�.+��� -    ��Name$ tbTitle��$��当前任务列表��$
��Bold"�&   System.Windows.Controls.DataGrid  -	   	  Name$	 dataGrid��
   AutoGenerateColumns
 .     
IsReadOnly . $��2��   Columns 4  .System.Windows.Controls.DataGridTemplateColumn  ,  &System.Windows.Controls.DataGridColumn   Header$  ��   CellTemplate �� .R��� -
   $��16��$��16��$��Center=�$��Center=�$��./Images/close.png��$��1q� �� 0  *System.Windows.Controls.DataGridTextColumn  $ 任务编号��1  +System.Windows.Controls.DataGridBoundColumn   Binding �� *TaskId+  $ 任务类型�� �� *
TaskType+ �� 	Converter# [   $ 任务状态�� �� .R��� ���� *	TaskState+# [  ���� *	TaskState+  $ 	服务器�� �� *Host+  $ 端口�� �� *Port+  $ FTP用户名�� �� *	FtpUser+  $ 	FTP路径�� �� *
TargetPath+  $ FTP文件名�� �� *	FtpFile+  $ 本地文件目录�� �� *
FilePath+�� -    ��Name$ lbLog��$��2��$y�	Collapsed=� $� IsVirtualizing .  $� VirtualizationMode$ 	Recycling=�