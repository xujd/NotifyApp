   M S B A M L   `   `   `  ����H  DWpfInfoPusher, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null-clr-namespace:WpfInfoPusherWpfInfoPusher  5   	   C&clr-namespace:WpfInfoPusher.ConvertersWpfInfoPusher.Converters  63       WpfInfoPusher.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   -localclr-namespace:WpfInfoPusher     5   	   <	converter&clr-namespace:WpfInfoPusher.Converters     63     � Icon$  ./Images/icon.ico��5       � Title$$ 信息交换识别推送系统��5   	   $	��350��6   $	��625��6)    � 	Resources 5	      -   'WpfInfoPusher.Converters.ColorConverter   colorConverter0   *WpfInfoPusher.Converters.TaskTypeConverter  typeConverter��%:   &	        &	        5
   
   6#     5   
   6&   5      .��5      �  � RowDefinitions 5   
   �� 5      $
��auto �6   �� 5      $
��auto �6   �� 5      .+�5   
   o� -   5         o�Name$
 menu��5      .@�5      m� -   5        $��首页��5      
 � Tag$
 Main��6#   6.   m� 5      -   5        $��创建任务��5      $ Create��6%   62   m� 5      -   5         m�Name$ 	menuStart��5      $��开始任务��6*   $ Start��68   6D   m� 5      -   5        $ menuStop��5      $��停止任务��6)   $y�	Collapsed=�67   $
 Stop��6N   6Y   m� 5      -   5        $��查看日志��5      $	 Log��6%   6/   m� 5      -   5        $��关于��5      $ About��6#   6/   5      �� 5   
    �� Orientation$ 
Horizontal=�6   $��1��6.   $
��11,5q�6;   .+�5      �� -   5         ��Name$ tbTitle��5      $��当前任务列表��6)   $
��Bold"�67   5      &   System.Windows.Controls.DataGrid  5   
   -	   5        	  Name$	 dataGrid��5      
   AutoGenerateColumns
 .     
IsReadOnly . $��2��6S      Columns 5      4  .System.Windows.Controls.DataGridTemplateColumn  5       ,  &System.Windows.Controls.DataGridColumn   Header$  ��6)      CellTemplate 5!      �� 5"      .R�5#      �� -
   5        $��16��5#   P   $��16��6[   $��Center=�6g   $��Center=�6�   $��./Images/close.png��6�   $��1q�6�    6@   �� 6�   5$      5&      0  *System.Windows.Controls.DataGridTextColumn  5'      $ 任务编号��6%   1  +System.Windows.Controls.DataGridBoundColumn   Binding 63   �� *TaskId+  5(      $ 任务类型��6%    63   �� *
TaskType+ �� 	Converter# [   5)      $ 任务状态��6)    5*      �� 5+      .R�5,      �� ��6(   �� *	TaskState+# [  ��6s   �� *	TaskState+5-      5/        50      $ 	服务器��6%    62   �� *Host+  51      $ 端口��6%    61   �� *Port+  52      $ FTP用户名��6%    65   �� *	FtpUser+  53      $ 	FTP路径��6%    64   �� *
TargetPath+  54      $ FTP文件名��6%    65   �� *	FtpFile+  55      $ 本地文件目录��6%    65   �� *
FilePath+57      �� 58   
   -   5         ��Name$ lbLog��58      $��2��6!   $y�	Collapsed=�6.    $� IsVirtualizing .  $� VirtualizationMode$ 	Recycling=�6r   5:      5;      5<      