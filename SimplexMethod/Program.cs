using System;

namespace SimplexMethod
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Нажмите Enter для решения задачи линейного программирования симлпексным методом");
            Console.ReadLine();
            Console.WriteLine("Будьте внимательны! Программа не защищена от ошибок ввода\n");
            bool isMaximization = true;
            int longestConstraintsCondition = 2; //Для ввода через командную строку
            double[] inputC = new double[] { 3, 4 }; //коэффициенты целевой функции
            double[,] inputX = new double[,] { 
                                                { 1, 1 },
                                                {-1, 1 },
                                                { 1, 0 },
                                                { 0, 1 }
                                             };
            string[] conditionsInput = new string[] { ">=", "<=", ">=", ">=" };
            double[] inputB = new double[] { 20, 20, 10, 5 }; 

            var simplex = new Simplex(longestConstraintsCondition, inputC, inputX, conditionsInput, inputB, isMaximization);
            var simplexTable = simplex.simplexTable;


           /* for(int j = 0; j < inputB.Length; j++)
            {
                for(int i = 0; i < 9; i++)
                {
                    Console.Write(simplexTable[j,i]);
                    Console.Write("   ");
                }
                Console.WriteLine("");
            }*/
            foreach (var e in simplexTable)
                Console.WriteLine(e);
        }
    }

    public class Simplex
    {
        public double[] simplexTable; //Имеет неправильный тип, пока что используется для тестирования результатов
        public int unlimited;

        public Simplex(int longestConstraintsCondition, double[] targetCoeffs, double[,] constraintsCoeffs, 
            string[] constraintsSigns, double[] constraintsValues, bool isMaximization)
        {
            int inputLength = constraintsCoeffs.GetLength(1); //количество коэффициентов в строках
            int height = constraintsCoeffs.GetLength(0); //количество коэффициентов в столбцах
            int countToAdd = 0;
            bool thereIsAMoreSign = false;          

            // ЗДЕСЬ НУЖНА ПРОВЕРКА НА МАКСИМИЗАЦИЮ ИЛИ МИНИМИЗАЦИЮ
            //Преобразует матрицу коэффициентов
            for (int i = 0; i < constraintsSigns.Length; i++)
                if (constraintsSigns[i] == ">=")
                { 
                    countToAdd++; //Для определения, сколько нужно ввести базисных переменных. Могут появиться и искусственные
                    thereIsAMoreSign = true;
                }

            int[] basicIndexes = new int[height]; //индексы базисных переменных внутри матрицы коэффициентов

            ResizeArray(ref constraintsCoeffs, height, inputLength + height + countToAdd); //Увеличение матрицы коэффициентов на
                                                                                           //количество базисных переменных

            for (int i = inputLength, j = 0; i < height + inputLength; i++, j++) //Запись базисных переменных в матрицу коэффициентов
            {
                if (constraintsSigns[j] == ">=")
                    constraintsCoeffs[j, i] = -1;
                else
                {
                    constraintsCoeffs[j, i] = 1;
                    basicIndexes[j] = i; //запись индексов базисных переменных внутри матрицы коэффициентов
                }                   
            }          

            for (int i = inputLength + height, j = 0; i < height + inputLength + countToAdd; i++, j++) //Ввод искусственных переменных,
                                                                                                       //называются также базисными
            {
                if (constraintsSigns[j] == ">=")
                {
                    constraintsCoeffs[j, i] = 1;
                    basicIndexes[j] = i;
                }
                else
                    i--;                                     
            }        

            ResizeArray(ref targetCoeffs, inputLength + height + countToAdd); //Добавление пустых ячеек в целевую функция до
                                                                              //длины матрицы коэффициентов
            // ЗДЕСЬ НУЖНА ПРОВЕРКА НА МАКСИМИЗАЦИЮ ИЛИ МИНИМИЗАЦИЮ
            // Поиск начального целевого вектора (обычно пишем над матрицей коэффициентов)
            if (thereIsAMoreSign) // Если в задаче был знак больше, то для максимизации необходимо ввести числа -M
                for (int i = 0; i < countToAdd; i++)
                    targetCoeffs[i + height + inputLength] = -1000000000000000;          

            //Расчет целевого значения
            double targetValue = 0; 
            
            for (int i = 0; i < height; i++)
            {
                targetValue = targetValue + targetCoeffs[basicIndexes[i]] * constraintsValues[i];
            }           

            double[] simplexDifference = new double[inputLength + height + countToAdd]; 

            //Поиск симплексной разницы (нижняя строка таблицы)
            for(int i = 0; i < simplexDifference.Length; i++)
            {
                for (int j = 0; j < height; j++)
                    simplexDifference[i] += targetCoeffs[basicIndexes[j]] * constraintsCoeffs[j, i];
                simplexDifference[i] -= targetCoeffs[i];
            }

            int mainColumnIndex = FindMainColumn(simplexDifference);
            int mainLineIndex = FindMainLine(height, constraintsValues, constraintsCoeffs, mainColumnIndex);

            //ОСНОВНОЙ АЛГОРИТМ
            //Пересчет всех значений до тех пор, пока не будут выполнены условия
            bool isEnd = false;
            while (isEnd)
            {
                for(int i = 0; i < inputLength; i++)
                {
                    if (simplexDifference[i] < 0)
                    {
                        if (isMaximization)
                        {
                            Maximization(ref constraintsCoeffs, ref simplexDifference, ref basicIndexes, height, ref constraintsValues,
                                inputLength, countToAdd, ref targetValue, ref targetCoeffs, ref mainLineIndex);
                            if (mainLineIndex == -1)
                            {
                                return ;
                            }
                        }
                        else
                        {
                            //Minimization();
                        }
                    }
                    else
                    {
                        //Нужны проверки
                        isEnd = true;
                    }
                }
            } 
            //организовать выход в main функцию
        }

        //Метод для решения задачи максимизации
        public static void Maximization(ref double [,] constraintsCoeffs, ref double[] simplexDifference, ref int[] basicIndexes, 
            int height, ref double[] constraintsValues, int inputLength, int countToAdd, ref double targetValue, 
            ref double[] targetCoeffs, ref int mainLineIndex) 
        {
            int mainColumnIndex = FindMainColumn(simplexDifference);            

            if (mainLineIndex == -1)
            {
                
                return;
            }
            //Пересчет массива B
            for (int j = 0; j < height; j++)
            {
                if (j == mainLineIndex) //По ведущей строке
                    constraintsValues[j] = constraintsValues[j] / constraintsCoeffs[mainLineIndex, mainColumnIndex];
                //По остальным строкам
                else constraintsValues[j] = constraintsValues[j] - (constraintsCoeffs[j, mainColumnIndex] *
                        constraintsValues[mainLineIndex]) / constraintsCoeffs[mainLineIndex, mainColumnIndex];
            }

            //Пересчет коэффициентов по правилу прямоугольника
            for (int i = 0; i < inputLength + height + countToAdd; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (i == mainColumnIndex)
                        continue;
                    else
                    {
                        if (j == mainLineIndex) //для ведущей строки
                            constraintsCoeffs[j, i] = constraintsCoeffs[j, i] / constraintsCoeffs[mainLineIndex, mainColumnIndex];
                        //По остальным строкам
                        else constraintsCoeffs[j, i] = constraintsCoeffs[j, i] - (constraintsCoeffs[j, mainColumnIndex] *
                                constraintsCoeffs[mainLineIndex, i]) / constraintsCoeffs[mainColumnIndex, mainLineIndex];
                    }
                }
            }

            //Пересчет ведущего столбца
            for (int i = 0; i < height; i++)
            {
                if (i == mainLineIndex)
                    constraintsCoeffs[mainLineIndex, mainColumnIndex] = 1;
                else constraintsCoeffs[i, mainColumnIndex] = 0;
            }

            //Указание нового базиса
            basicIndexes[mainLineIndex] = mainColumnIndex;

            //Пересчет целевой функции
            for (int i = 0; i < height; i++)
            {
                targetValue = targetValue + targetCoeffs[basicIndexes[i]] * constraintsValues[i];
            }

            //пересчет симплексной разности
            for (int i = 0; i < simplexDifference.Length; i++)
            {
                for (int j = 0; j < height; j++)
                    simplexDifference[i] += targetCoeffs[basicIndexes[j]] * constraintsCoeffs[j, i];
                simplexDifference[i] -= targetCoeffs[i];
            }
        }

        private static void ResizeArray(ref double[,] arr, int newM, int newN)
        {
            double[,] newArray = new double[newM, newN];
            for (int m = 0; m < arr.GetLength(0); m++)
                for (int n = 0; n < arr.GetLength(1); n++)
                    newArray[m, n] = arr[m, n];
            arr = newArray;
        }

        private static void ResizeArray(ref double[] arr, int newN)
        {
            double[] newArray = new double[newN];
            for (int n = 0; n < arr.Length; n++)
                newArray[n] = arr[n];
            arr = newArray;
        }

        private static int FindMainColumn(double[] simplexDifference)
        {
            int columnIndex = 0;
            double min = double.MaxValue;
            for (int i = 0; i < simplexDifference.Length; i++)
                if (min > simplexDifference[i] * (-1))
                {
                    min = simplexDifference[i] * (-1);
                    columnIndex = i - 1;
                }
            return columnIndex;
        }

        private static int FindMainLine(int height, double[] constraintsValues, 
            double[,] constraintsCoeffs, int mainColumnIndex)
        {
            int lineIndex = -1;
            var minBuffer = new double[height];
            for (int i = 0; i < height; i++)
                minBuffer[i] = constraintsValues[i] / constraintsCoeffs[i, mainColumnIndex];

            double min = double.MaxValue;
            for (int i = 0; i < height; i++)
                if (minBuffer[i] <= 0 || double.IsInfinity(minBuffer[i]))
                {
                    continue;
                }
                else if(min > minBuffer[i])
                {
                    min = minBuffer[i];                                   //ГДЕ ТО ТУТ ВЫРОЖДЕННОЕ РЕШЕНИЕ
                    lineIndex = i;
                }
            return lineIndex;
        } 
    }
} 

