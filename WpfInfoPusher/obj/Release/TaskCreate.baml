   M S B A M L   `   `   `  ���� H  DWpfInfoPusher, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null-clr-namespace:WpfInfoPusherWpfInfoPusher  C&clr-namespace:WpfInfoPusher.ConvertersWpfInfoPusher.Converters      WpfInfoPusher.TaskCreate   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     -localclr-namespace:WpfInfoPusher     7conv&clr-namespace:WpfInfoPusher.Converters       � Icon$  ./Images/icon.ico�� � Title$ 创建任务��$	��340��$	��400�� � 
ResizeMode$ NoResize=� � 	Resources /   )WpfInfoPusher.Converters.VisibleConverter   visibleConverter��%   &	          .��� $��11q� � RowDefinitions �� $
��auto ��� $
��auto ��� $
��auto ��� $
��auto ��� $
��auto ��� $
��auto ��� $
��auto ��� $��* � � ColumnDefinitions �� $
��auto ��� �� $
��auto �.+��� $��文件目录：�� �� Padding$ 0,3,0,0q�$��Right=�$��0��$��5q�� $��1��$��5q� �� $
��auto ��� .+��� -    ��Name$ btnOpen��$	��...��$��30��$��23��$��Center=�$
��Left=��� -    ��Name$ 
tbFilePath��$�� ��$��0,3,0,0q�$��23��$	��1,0q�	 �� 
IsReadOnly	 . $��1���� $��FTP服务器：��$ 0,3,0,0q�$��Right=�$��1��$��5q�y��� 
 �� ElementName$
 rbLed�� �� Path$ 	IsChecked� �� 	Converter# [  �� -   $ tbHost��$�� ��$��1��$��1��$��0,3,0,0q�$��5q�$��23��y��� $
 rbLed��$ 	IsChecked�# [  �� $��FTP用户名：��$ 0,3,0,0q�$��Right=�$��2��$��5q�y��� $
 rbLed��$ 	IsChecked�# [  �� -   $ tbUser��$�� ��$��2��$��1��$��0,3,0,0q�$��5q�$��23��y��� $
 rbLed��$ 	IsChecked�# [  �� $��FTP密码：��$ 0,3,0,0q�$��Right=�$��3��$��5q�y��� $
 rbLed��$ 	IsChecked�# [  G� -    G�Name$ 
tbPassword�� G� Password$  ��$��3��$��1��$��0,3,0,0q�$��5q�$��23��y��� $
 rbLed��$ 	IsChecked�# [  �� $��FTP路径：��$ 0,3,0,0q�$��Right=�$��4��$��5q�y��� $
 rbLed��$ 	IsChecked�# [  �� -   $ tbTargetPath��$�� ��$��4��$��1��$��0,3,0,0q�$��5q�$��23��y��� $
 rbLed��$ 	IsChecked�# [  �� $��FTP文件名：��$ 0,3,0,0q�$��Right=�$��5��$��5q�y��� $
 rbLed��$ 	IsChecked�# [  �� -   $ 	tbFtpFile��$�� ��$��5��$��1��$��0,3,0,0q�$��5q�$��23��y��� $
 rbLed��$ 	IsChecked�# [  �� $��UDP服务器：��$ 0,3,0,0q�$��Right=�$��1��$��5q�y��� $
 rbMsg��$ 	IsChecked�# [  �� -   $ 	tbHostUdp��$�� ��$��1��$��1��$��0,3,0,0q�$��5q�$��23��y��� $
 rbMsg��$ 	IsChecked�# [  �� $��UDP端口：��$ 0,3,0,0q�$��Right=�$��2��$��5q�y��� $
 rbMsg��$ 	IsChecked�# [  �� -	   $ 	tbPortUdp��$�� ��$��2��$��1��$��0,3,0,0q�$��5q�$��23��y��� $
 rbMsg��$ 	IsChecked�# [  �� $��解析算法：��$ 0,3,0,0q�$��Right=�$��6��$��5q��� -
    ��Name$ spAlg�� �� Orientation$ 
Horizontal=�$��6��$��1��$��6q�$��23��.+�	� -    	�Name$ rbLed�� d� 	IsChecked$
 TrueY�$��	LED显示��$��Center=�	� -   $ rbMsg��$��报文分析��$��Center=�$��11,0,0,0q��� $ 
Horizontal=�$
��0,11q�$��7��$��3��$��Right=�$��Bottom=�.+��� -   $ btnOK��$��确定��$��75��$��23���� -   $ 	btnCancel��$��取消��$��11,0,0,0q�$��75��$��23��