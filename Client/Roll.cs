using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class Roll : Form
    {
        // 骰子图片数组
        private readonly Image[] diceImages;

        // 随机数生成器
        private readonly Random random = new Random();

        // 是否正在摇骰子
        private bool isRolling = false;

        private int result;

        public Roll()
        {
            InitializeComponent();

            // 加载骰子图片
            diceImages = new Image[]
            {
                Properties.Resources.dice1,
                Properties.Resources.dice2,
                Properties.Resources.dice3,
                Properties.Resources.dice4,
                Properties.Resources.dice5,
                Properties.Resources.dice6
            };

            // 初始显示骰子1
            dicePictureBox.Image = diceImages[0];
        }

        private async void rollButton1_Click(object sender, EventArgs e)
        {
            // 如果正在摇骰子，则忽略点击
            if (isRolling) return;

            isRolling = true;
            rollButton1.Visible = false;
            rollButton2.Visible = false;

            // 摇骰子动画效果
            for (int i = 0; i < 5; i++)
            {
                int randomFace = random.Next(0, 6);
                dicePictureBox.Image = diceImages[randomFace];
                await Task.Delay(50 + i * 10); // 逐渐减慢
            }

            // 最终结果
            result = random.Next(0, 6);
            dicePictureBox.Image = diceImages[result];

            isRolling = false;
        }

        private async void rollButton2_Click(object sender, EventArgs e)
        {
            // 如果正在摇骰子，则忽略点击
            if (isRolling) return;

            isRolling = true;
            rollButton1.Visible = false;
            rollButton2.Visible = false;

            // 摇骰子动画效果
            for (int i = 0; i < 10; i++)
            {
                int randomFace = random.Next(0, 6);
                dicePictureBox.Image = diceImages[randomFace];
                await Task.Delay(50 + i * 10); // 逐渐减慢
            }

            // 最终结果
            result = random.Next(0, 6);
            dicePictureBox.Image = diceImages[result];
            isRolling = false;
        }
    }
}