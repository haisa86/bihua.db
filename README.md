## 说明
库文件需移步到百度网盘：https://pan.baidu.com/s/1ocBNxhbhx2NaYCOU3Sn15g  提取码：sir9

附赠爬虫工具：BiHuaCrawler，使用.net core2.1构建，依赖MongoDB.Driver.dll和Newtonsoft.Json

此库共收录了6760个常用汉字的拼音、首部、笔画、笔顺

此库全部数据来源于哈童网校：http://xue.hahaertong.com/index.php?app=chinese，仅供学习不可用于商业用途

使用mongodb集群构建该库，2个分片 + 配置服务 + 路由服务，mongodb版本为V4.0.4

其中分片和配置服务都是配置为3个重复集进程，路由服务只有一个进程
├── 分片1：rs1
│   └── shard1-1: 重复集1
│   └── shard1-2: 重复集2
│   └── shard1-3: 重复集3
├── 分片2: rs2
│   └── shard2-1: 重复集1
│   └── shard2-2: 重复集2
│   └── shard2-3: 重复集3
├── 配置服务: cfg
│   └── config1-1: 重复集1
│   └── config1-2: 重复集2
│   └── config1-3: 重复集3
├── 路由服务
│   └── rout: 路由进程

每个文件夹中都有一个mongodb配置文件：mongod.cfg，在Windows系统中可以以Windows服务创建mongodb实例
请按照具体环境，更改配置文件中dbpath、logpath、configdb的值

分片和配置服务安装命令模板：
mongod --config "<目录路径>\mongod.cfg" --install --serviceName "<Windos服务名称>"

路由服务服务安装命令模板：
mongos --config "<目录路径>\mongod.cfg" --install --serviceName "<Windos服务名称>"


## 库文档说明
├── ChineseChar：汉字文档
│   └── Unicode: ushort 字符的unicode值
│   └── Text: string 字符长什么样
│   └── CreatedTime: DateTime 入库时间
│   └── ModifiedTime: DateTime 最后更改时间
│   └── BuShou: string 该汉字的部首
│   └── RectSize: int 画布原始尺寸大小
│   └── Pinyins: string[] 该汉字的全部拼音
│   └── BiHuas: List<List<int[]>> 该汉字的笔画图案绘制点，第一层为汉字的单个笔画，第二层为单个笔画图案的全部绘制点，int[]的长度为2，表示绘制点在画布上的x,y坐标，将全部绘制点用直线连起来就形成笔画的图案
│   └── BiShuns: List<List<int[]>> 该汉字的笔顺图案绘制点，第一层为汉字的单个笔顺，第二层为单个笔顺图案的全部绘制点，int[]的长度为2，表示绘制点在画布上的x,y坐标，将全部绘制点用直线连起来就形成笔顺的轨迹




