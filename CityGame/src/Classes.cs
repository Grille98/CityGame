﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
//using System.Drawing.Imaging;
using System.Diagnostics;
using System.Windows.Forms;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using GGL;

namespace CityGame
{
    public class World
    {
        private GameObject[] gameObjects;
        private GameResources[] resources;
        private Random rnd;
        private int width;
        public int Width { get { return width; } }
        private int height;
        public int Height { get { return height; } }
        public byte[] Typ;
        public byte[] Zone;
        public byte[] Tile;
        public byte[] Ground;
        public byte[] ReferenceX;
        public byte[] ReferenceY;
        public byte[] Version;
        public byte[] vertexHeight;
        public byte[] vertexTexture;
        public int[,] Data;

        private int[] objectCounter;
        public int[] ObjectCounter { get { return objectCounter; } }

        private bool loadMode = false;

        public World(GameObject[] gameObjects, GameResources[] resources)
        {
            this.gameObjects = gameObjects;
            this.resources = resources;

        }
        public void BuildWorld(int width, int height)
        {

            this.width = width;
            this.height = height;
            Ground = new byte[width * height];
            Typ = new byte[width * height];
            Tile = new byte[width * height];
            ReferenceX = new byte[width * height];
            ReferenceY = new byte[width * height];
            Version = new byte[width * height];
            vertexHeight = new byte[(width + 1) * (height + 1)];
            vertexTexture = new byte[(width + 1) * (height + 1)];
            Data = new int[10, width * height];
            objectCounter = new int[256];
            rnd = new Random(1000);
            //rndMap();
        }
        private void rndMap()
        {
            rnd = new Random(1000);

            for (int i = 0; i < width * height; i++)
            {
                //vertexHeight[i] = (byte)(rnd.NextDouble() * 1);
                if (rnd.NextDouble() > 0.5)
                {
                    Ground[i] = 1;
                    if (rnd.NextDouble() < 0.5)
                    {
                        Ground[i] = 2;
                    }
                }
                if (rnd.NextDouble() < 0.01)
                {
                    if (rnd.NextDouble() < 0.55) Build(3, i);
                    else Build(4, i);
                    Ground[i] = 4;
                }
            }
            for (int ix = 1; ix < width - 1; ix++)
            {
                for (int iy = 1; iy < height - 1; iy++)
                {
                    if (rnd.NextDouble() < 0.6)
                    {
                        int offset = ix + iy * width;
                        if (Typ[offset] == 0 && (Typ[offset + 1] == 4 || Typ[offset - 1] == 4 || Typ[offset + width] == 4 || Typ[offset - width] == 4))
                        {
                            Build(4, offset);
                            Ground[offset] = 4;
                        }
                        else if (Typ[offset] == 0 && (Typ[offset + 1] == 3 || Typ[offset - 1] == 3 || Typ[offset + width] == 3 || Typ[offset - width] == 3))
                        {
                            Build(3, offset);
                            Ground[offset] = 4;
                        }
                    }
                }
            }
            for (int ix = 1; ix < width - 1; ix++)
            {
                for (int iy = 1; iy < height - 1; iy++)
                {
                    int offset = ix + iy * width;
                    if (Ground[offset] != 4 && (Ground[offset + 1] == 4 || Ground[offset - 1] == 4 || Ground[offset + width] == 4 || Ground[offset - width] == 4))
                    {
                        if (Typ[offset] == 0 && (Typ[offset + 1] == 4 || Typ[offset - 1] == 4 || Typ[offset + width] == 4 || Typ[offset - width] == 4))
                        {
                            Build(4, offset);
                            Ground[offset] = 3;
                        }
                        else if (Typ[offset] == 0 && (Typ[offset + 1] == 3 || Typ[offset - 1] == 3 || Typ[offset + width] == 3 || Typ[offset - width] == 3))
                        {
                            Build(3, offset);
                            Ground[offset] = 3;
                        }
                    }
                }
            }
            for (int ix = 1; ix < width - 1; ix++)
            {
                for (int iy = 1; iy < height - 1; iy++)
                {
                    int offset = ix + iy * width;
                    //if (rnd.NextDouble() < 0.05) Build(3, offset);
                    if (rnd.NextDouble() < 0.001) Ground[offset] = 5;
                    if (rnd.NextDouble() < 0.00002)
                    {
                        //Build(1, offset);

                        float posX = ix;
                        float posY = iy;
                        float addX = 0f;
                        float addY = 0;
                        for (int i = 0; i < 2000; i++)
                        {
                            addX += (float)(rnd.NextDouble() * 0.5 - 0.25f);
                            addY += (float)(rnd.NextDouble() * 0.5 - 0.25f);
                            if (Math.Abs(addX) > 1) addX *= 0.75f;
                            if (Math.Abs(addY) > 1) addY *= 0.75f;
                            posX += addX;
                            posY += addY;
                            if ((posX > 1 && posY > 1 && posX < width - 2 && posY < height - 2))
                            {
                                Build(1, (int)posX, (int)posY);

                                Build(1, (int)posX + 1, (int)posY);
                                Build(1, (int)posX - 1, (int)posY);
                                Build(1, (int)posX, (int)posY + 1);
                                Build(1, (int)posX, (int)posY - 1);
                            }
                            else break;
                        }

                    }

                }
            }
        }

        public bool CanBuild(byte typ, int x, int y)
        {
            return CanBuild(typ, x + y * width);
        }
        public bool CanBuild(byte typ, int pos)
        {

            int x = pos % width;
            int y = (pos - x) / width;

            int size = gameObjects[typ].Size;
            if (x < 0 || y < 0 || x > width - size || y > width - size) return false;
            if (TestTyp(typ, pos) > 1) return false;
            if (gameObjects[typ].CanBuiltOnTyp.Length == 0) return true;
            bool returnValue = true;



            for (int ix = 0; ix < size; ix++)
            {
                for (int iy = 0; iy < size; iy++)
                {
                    int curPos = pos + ix + iy * width;
                    curPos = curPos - ReferenceX[curPos] - ReferenceY[curPos] * width;
                    bool matching = false;
                    for (int i = 0; i < gameObjects[typ].CanBuiltOnTyp.Length; i++)
                    {
                        matching = matching || Typ[curPos] == gameObjects[typ].CanBuiltOnTyp[i];
                    }
                    returnValue = returnValue && matching;
                }
            }
            return returnValue;
        }

        private void buildEffects(byte typ, int pos)
        {
            buildEffects(typ, pos, true);
        }
        private void buildEffects(byte typ,int pos,bool add)
        {
            int x = pos % width;
            int y = (pos - x) / width; 

            for (int i = 0; i < gameObjects[typ].AreaPermanent.GetLength(0); i++)
            {
                int typSize = gameObjects[typ].Size;
                int dataTyp = gameObjects[typ].AreaPermanent[i,0];
                int dataSize = gameObjects[typ].AreaPermanent[i, 1];
                int dataValue = gameObjects[typ].AreaPermanent[i, 2];
                if (add) objectCounter[typ]++;
                else{dataValue = -dataValue;objectCounter[typ]--;}

                int startX = x - dataSize;
                if (startX < 0) startX = 0;
                int startY = y - dataSize;
                if (startY < 0) startY = 0;
                int endX = x + dataSize + typSize;
                if (endX > width) endX = width;
                int endY = y + dataSize + typSize;
                if (endY > height) endY = height;

                for (int ix = startX; ix < endX; ix++)
                {
                    for (int iy = startY; iy < endY; iy++)
                    {
                        int curPos = ix + iy * width;
                        Data[dataTyp, curPos] += dataValue;
                    }
                }
            }
        }

        public void Build(byte typ, int x, int y)
        {
            Build(typ, x + y * width);
        }
        public void Build(byte typ, int pos)
        {
            buildEffects(typ, pos, true);

            int x = pos % width;
            int y = (pos - x) / width; 

            int size = gameObjects[typ].Size;

            if (x < 0 || y < 0 || x > width - size || y > width - size) return;

            // clear & ref
            for (int ix = 0; ix < size; ix++)
            {
                for (int iy = 0; iy < size; iy++)
                {
                    int curPos = pos + ix + iy * width;
                    Clear(curPos);
                    ReferenceX[curPos] = (byte)ix;
                    ReferenceY[curPos] = (byte)iy;
                }
            }

            //build
            Typ[pos] = typ;
            Version[pos] = (byte)(rnd.NextDouble() * gameObjects[typ].Diversity);

            //set Data


            //autoTile
            for (int ix = -1; ix <= size; ix++)
            {
                for (int iy = -1; iy <= size; iy++)
                {
                     autoTile(pos + ix + iy * width);
                }
            }
        }

        private void autoTile(int pos)
        {
            int x = pos % width;
            int y = (pos - x) / width;
            if (x >= 0 && y >= 0 && x <= width - 1 && y <= height - 1)
            {
                Tile[pos] = (byte)AutoTile(Typ[pos], pos);
            }
        }
        public int AutoTile(byte typ,int pos)
        {
            int x = pos % width;
            int y = (pos - x) / width;


            byte code = 0;
            if (gameObjects[typ].GraphicMode == 1)
            {
                if (Typ[pos - 1] == typ) code += 1;//l
                if (Typ[pos + Width] == typ) code += 2;//u
                if (Typ[pos + 1] == typ) code += 4;//r
                if (Typ[pos - Width] == typ) code += 8;//o
            }
            else
            {
                int[] graphicNeighbors = gameObjects[typ].graphicNeighbors;
                bool l = true, u = true, r = true, o = true;
                for (int i = 0; i < graphicNeighbors.Length; i++)
                {
                    if (x > 0 && Typ[pos - 1] == graphicNeighbors[i]) { code += 1; l = false; }//l
                    if (x+1 < width && Typ[pos + 1] == graphicNeighbors[i]) { code += 4; r = false; }//r
                    if (y > 0 && Typ[pos - Width] == graphicNeighbors[i]) { code += 8; o = false; }//o
                    if (y+1 < height && Typ[pos + Width] == graphicNeighbors[i]) { code += 2; u = false; }//u
                }
            }
            return code;
        }
        public void Clear(int pos) 
        {
            pos = pos - ReferenceX[pos] - ReferenceY[pos] * width;
            int size = gameObjects[Typ[pos]].Size;
            buildEffects(Typ[pos], pos, false);
            for (int ix = 0; ix < size; ix++)
            {
                for (int iy = 0; iy < size; iy++)
                {
                    int curPos = pos + ix + iy * width;
                    Typ[curPos] = 0;
                    ReferenceX[curPos] = 0;
                    ReferenceY[curPos] = 0;
                }
            }
            //return pos;
        }

        public int TestTyp(byte typ, int pos)
        {
            int retValue = 0;

            int size = gameObjects[typ].Size;
            for (int i = 0; i < gameObjects[typ].AreaDependent.GetLength(0); i++)
            {
                int dataTyp = gameObjects[typ].AreaDependent[i,0];
                int dataMin = gameObjects[typ].AreaDependent[i, 1];
                int dataMax = gameObjects[typ].AreaDependent[i, 2];
                int dataValue = gameObjects[typ].AreaDependent[i, 3];
                int dataInvert = gameObjects[typ].AreaDependent[i, 4];

                bool fuba = true;
                for (int ix = 0; ix < size; ix++)
                {
                    for (int iy = 0; iy < size; iy++)
                    {
                        int curPos = pos + ix + iy * width;

                        int curValue = Data[dataTyp,curPos];

                        if (dataInvert == 0)
                        {
                            fuba = fuba & !(curValue < dataMin || curValue > dataMax);
                        }
                        else
                        {
                            fuba = fuba & (curValue < dataMin || curValue > dataMax);
                        }
                    }
                }
                if (fuba && retValue < dataValue) retValue = dataValue;
            }
            return retValue;
        }
        public void UpdateField(int pos)
        {
            if (ReferenceX[pos] != 0 || ReferenceY[pos] != 0) return;
            byte typ = Typ[pos];
            int result = TestTyp(typ, pos);
            if (result == 0) return;
            else if (result == 1) updateTyp(typ, pos, gameObjects[typ].UpgradeTyp);
            else if (result == 2) updateTyp(typ, pos, gameObjects[typ].DowngradeTyp);
            else if (result == 3) updateTyp(typ, pos, gameObjects[typ].DemolitionTyp);
            else if (result == 4) updateTyp(typ, pos, gameObjects[typ].DecayTyp);
            else if (result == 5) updateTyp(typ, pos, gameObjects[typ].DestroyTyp);
            else if (result == 6) Clear(pos);
            return;
        }
        private void updateTyp(byte typ, int pos,int[] replace)
        {
            int newTyp = (int)(replace.Length * rnd.NextDouble());
            if (gameObjects[typ].Size == gameObjects[replace[newTyp]].Size)
            {
                Build((byte)replace[0], pos);
            }
            else if (gameObjects[replace[newTyp]].Size == 1)
            {
                int size = gameObjects[typ].Size;
                for (int ix = 0;ix < size; ix++)
                {
                    for (int iy = 0; iy < size; iy++)
                    {    
                        Build((byte)replace[newTyp], pos + ix + iy * width);
                    }
                }
            }
            return;
        }


        //private byte[] decompressByte(byte[] input)
        //{
        //    //byte[] returnData = new byte[index];
        //    //for (int i = 0; i < returnData.Length; i++)
        //    //{
        //    //    returnData[i] = saveData[i];
        //    //}
        //    //return returnData;
        //}
        private byte[] compressByte(byte[] input)
        {
            //byte[] saveData = new byte[input.Length];
            //int index = 0;
            //byte lastData = input[0];
            //int dataLenght = 0;
            //for (int i = 1; i < input.Length; i++)
            //{
            //    if (input[i] == lastData && dataLenght < 16)
            //    {
            //        dataLenght++;
            //    }
            //    else
            //    {
            //        saveData[index] = (byte)(dataLenght | lastData << 4);
            //        //saveData[index + 1] = lastData;
            //        dataLenght = 0;
            //        index += 1;
            //        lastData = input[i];
            //    }
            //}
            //byte[] returnData = new byte[index];
            //for (int i = 0; i < returnData.Length; i++)
            //{
            //    returnData[i] = saveData[i];
            //}
            //return returnData;

            byte[] saveData = new byte[input.Length * 2];
            int index = 0;
            byte lastData = input[0];
            int dataLenght = 0;
            for (int i = 1; i < input.Length; i++)
            {
                if (input[i] == lastData && dataLenght < 255)
                {
                    dataLenght++;
                }
                else
                {
                    saveData[index] = (byte)dataLenght;
                    saveData[index + 1] = lastData;
                    dataLenght = 0;
                    index += 2;
                    lastData = input[i];
                }
            }
            byte[] returnData = new byte[index];
            for (int i = 0; i < returnData.Length; i++)
            {
                returnData[i] = saveData[i];
            }
            return returnData;

        }
        private byte[] addByte(byte[] input1,byte[] input2) 
        {
            byte[] returnData = new byte[input1.Length + input2.Length];
            int index = 0;
            for (int i = 0; i < input1.Length; i++)
            {
                returnData[index++] = input1[i];
            }
            for (int i = 0; i < input2.Length; i++)
            {
                returnData[index++] = input2[i];
            }
            return returnData;
        }
        public void Save(string path)
        {

            byte[] saveData = new byte[8*16];
            saveData = addByte(saveData, compressByte(Ground));
            saveData = addByte(saveData, compressByte(Typ));
            saveData = addByte(saveData, compressByte(Version));
            File.WriteAllBytes(path, saveData);

            //File.WriteAllBytes(path, addByte(compressByte(Typ), compressByte(Tile)));
        }
        public void Load(string path) { }
        public void GenerateMap(Image map)
        {
            Random rnd = new Random(1000);
            Console.WriteLine(map.Width);
            BuildWorld(map.Width, map.Height);
            LockBitmap data = new LockBitmap((Bitmap)map, true);
            byte[] rgbData = data.getData();
            Console.WriteLine("Bitmap: " + (int)(map.Width * map.Height) + " Map: " + rgbData.Length/4);
            loadMode = true;
            for (int i = 0; i < rgbData.Length/4; i++)
            {
                if (rnd.NextDouble() > 0.5)
                {
                    Ground[i] = 1;
                    if (rnd.NextDouble() < 0.5)
                    {
                        Ground[i] = 2;
                    }
                }

                if (rgbData[i * 4 + 0] == 255)
                {
                    Build(1, i);
                }
                else if (rgbData[i * 4 + 0] == 254)
                {
                    Build(2, i);
                }
                else if (rgbData[i * 4 + 1] == 80)
                {
                    Build(3, i);
                    Ground[i] = 3;
                }
                else if (rgbData[i * 4 + 1] == 100)
                {
                    Build(4, i);
                    Ground[i] = 3;
                }
                else if (rgbData[i * 4 + 1] == 160)
                {
                    Build(5, i);
                    Ground[i] = 3;
                }

                if (rnd.NextDouble() < 0.001) Ground[i] = 5;
                //if (rgbData[i * 4 + 1] == 128) Build(1, i);
            }
            loadMode = false;
        }
    }

    public class GameObject
    {
 
        private string name;
        public int BuildMode;
        private Texture[,] texture;
        public Texture[,] Texture { get { return texture; } }
        private int diversity;
        public int Diversity { get { return diversity; } }
        private byte size;
        public byte Size { get { return size; } }
        private Texture[] ground;
        public Texture[] Ground { get { return ground; } }
        private int groundMode;
        public int GroundMode { get { return groundMode; } }

        private int graphicMode;  //0=nothing, 1=self, 2=useArray;
        public int GraphicMode { get { return graphicMode; } }
        public int[] graphicNeighbors;

        public int[] UpgradeTyp;
        public int[] DowngradeTyp;
        public int[] DemolitionTyp;
        public int[] DecayTyp;
        public int[] DestroyTyp;
        public int[] CanBuiltOnTyp;          //[typ]

        //effects: 0=up, 1=down, 2=demolition, 3=deacy, 4=destroy 5=entf//
        //importance: 0=canNotWork, 1=canWork//
        public int[,] AreaPermanent;        //[[typ,size,value]]
        public int[,] AreaDependent;      //[[typ,minValue,maxValue,effects]]
        public int[,] ResourcesBuild;     //[[typ,value,importance]]
        public int[,] ResourcesPermanent; //[[typ,value,importance]]
        public int[,] ResourcesMonthly;   //[[typ,value,importance]]
        public int[,] ResourcesDependent; //[[typ,minValue,maxValue,effects]]

        public void LoadBasic(string name, string path, string groundPath, int buildMode,int slopeMode, int diversity, int size,int groundMode, int graphicMode, int[] graphicNeighbors)
        {

            this.name = name;
            this.diversity = diversity;
            this.size = (byte)size;

            this.BuildMode = buildMode;
            this.groundMode = groundMode;
            this.graphicMode = graphicMode;
            this.graphicNeighbors = graphicNeighbors;

            if (groundPath != "-")
            {
                ground = new Texture[1];
                if (File.Exists(groundPath + "_g.png")) ground[0] = new Texture(groundPath+"_g.png");
                else ground[0] = new Texture(groundPath + ".png"); 
            }
            if (path != "-")
            {
                texture = new Texture[diversity, 16];
                for (int i = 0; i < diversity; i++)
                {
                    if (graphicMode == 0)
                    {
                        if (!File.Exists(path + "_" + i + "_0.png")) texture[i, 0] = new Texture(path + "_" + i + ".png"); 
                        else texture[i, 0] = new Texture(path + "_" + i + "_0.png");
                    }
                    else
                    {
                        for (int i2 = 0; i2 < 16; i2++)
                        {
                            if (!File.Exists(path + "_" + i + "_" + i2 + ".png")) texture[i, i2] = texture[i, 0];
                            else texture[i, i2] = new Texture(path + "_" + i + "_" + i2 + ".png");
                        }
                    }
                }
            }
        }
        public void LoadTypRefs(int[] upgradeTyp, int[] downgradeTyp, int[] demolitionTyp, int[] decayTyp, int[] destroyTyp, int[] CanBuiltOnTyp) 
        {
            this.UpgradeTyp = upgradeTyp;
            this.DowngradeTyp = downgradeTyp;
            this.DemolitionTyp = demolitionTyp;
            this.DecayTyp = decayTyp;
            this.DestroyTyp = destroyTyp;
            this.CanBuiltOnTyp = CanBuiltOnTyp;
        }
        public void LoadSimData(int[,] AreaPermanent,int[,] AreaDependent,int[,] ResourcesBuild,int[,] ResourcesPermanent,int[,] ResourcesMonthly,int[,] ResourcesDependent)
        {
            this.AreaPermanent = AreaPermanent;
            this.AreaDependent = AreaDependent;
            this.ResourcesBuild = ResourcesBuild;
            this.ResourcesPermanent = ResourcesPermanent;
            this.ResourcesMonthly = ResourcesMonthly;
            this.ResourcesDependent = ResourcesDependent;
        }
    }

    public class GameResources 
    {
        private string name;
        private bool canBeNegative;
        private bool storable;
        private int value;
        private int addValue;
        public int AddValue
        {
            set
            {
            }
            get
            {
                return AddValue;
            }
        }
        private int storeSize;


        public GameResources()
        {
        }
        public void Load(string name, bool canBeNegative, bool storable)
        {
            this.name = name;
            this.canBeNegative = canBeNegative;
            this.storable = storable;
        }
        public void Update()
        {
            value += AddValue;
        }

    }

    public class Camera
    {
        private World world;
        private Control sender;
        public int Speed = 30;
        public int Size = 64;
        public int PosX = 0;
        public int PosY = 0;

        public int DetailX = 0;
        public int DetailY = 0;

        public Camera(World world,Control sender)
        {
            this.world = world;
            this.sender = sender;
        }
        public void Move(int x, int y)
        {
            DetailX += x;
            DetailY += y;
            while (DetailX >= Size) { DetailX -= Size; PosX++; }
            while (DetailY >= Size) { DetailY -= Size; PosY++; }
            while (DetailX < 0) { DetailX += Size; PosX--; }
            while (DetailY < 0) { DetailY += Size; PosY--; }
        }
        public int GetCenter()
        {
            int x = sender.Width / Size - PosX;
            int y = sender.Height / Size - PosY;
            return x+y*world.Width;
        }
        public void SetCenter(int pos)
        {
            PosX = pos % world.Width - sender.Width / Size;
            PosY = pos / world.Height - sender.Height / Size;
        }
    }
}
