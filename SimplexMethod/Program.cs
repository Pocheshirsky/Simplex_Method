using System;

namespace SimplexMethod
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Нажмите Enter для решения задачи линейного программирования симлпексным методом");
            Console.ReadLine();

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
           // var answer = simplex.Maximization(simplexTable);
           // Console.WriteLine(answer);

            //foreach (var e in simplexTable)
                Console.WriteLine(simplexTable);
        }
    }

    public class Simplex
    {
        public int simplexTable; //Имеет неправильный тип, пока что используется для тестирования результатов

        public Simplex(int longestConstraintsCondition, double[] targetCoeffs, double[,] constraintsCoeffs, 
            string[] constraintsSigns, double[] constraintsValues, bool isMaximization)
        {
            int inputlength = constraintsCoeffs.GetLength(1); //количество коэффициентов в строках
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

            ResizeArray(ref constraintsCoeffs, height, inputlength + height + countToAdd); //Увеличение матрицы коэффициентов на
                                                                                           //количество базисных переменных

            for (int i = inputlength, j = 0; i < height + inputlength; i++, j++) //Запись базисных переменных в матрицу коэффициентов
            {
                if (constraintsSigns[j] == ">=")
                    constraintsCoeffs[j, i] = -1;
                else
                {
                    constraintsCoeffs[j, i] = 1;
                    basicIndexes[j] = i; //запись индексов базисных переменных внутри матрицы коэффициентов
                }
                    
            }          

            for (int i = inputlength + height, j = 0; i < height + inputlength + countToAdd; i++, j++) //Ввод искусственных переменных,
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

            ResizeArray(ref targetCoeffs, inputlength + height + countToAdd); //Добавление пустых ячеек в целевую функция до
                                                                              //длины матрицы коэффициентов
            // ЗДЕСЬ НУЖНА ПРОВЕРКА НА МАКСИМИЗАЦИЮ ИЛИ МИНИМИЗАЦИЮ
            // Поиск начального целевого вектора (обычно пишем над матрицей коэффициентов)
            if (thereIsAMoreSign) // Если в задаче был знак больше, то для максимизации необходимо ввести числа -M
                for (int i = 0; i < countToAdd; i++)
                    targetCoeffs[i + height + inputlength] = -1000000000000000;          

            //Расчет начального вектора симплексной разницы
            double targetValue = 0; // Сама симлпексная разность
            
            for (int i = 0; i < height; i++)
            {
                targetValue = targetValue + targetCoeffs[basicIndexes[i]] * constraintsValues[i]; //расчет целевого значения
            }           

            double[] simplexDifference = new double[inputlength + height + countToAdd]; //будет передаваться в функции
                                                                                        //максимизации или минимизации
            //Поиск симплексной разницы (нижняя строка таблицы)
            for(int i = 0; i < simplexDifference.Length; i++)
            {
                for (int j = 0; j < height; j++)
                    simplexDifference[i] += targetCoeffs[basicIndexes[j]] * constraintsCoeffs[j, i];
                simplexDifference[i] -= targetCoeffs[i];
            }

            int mainColumnIndex = FindMainColumn(simplexDifference);
            int mainLineIndex = FindMainLine(height, constraintsValues, constraintsCoeffs, mainColumnIndex);
            
            //В ЭТОТ МОМЕНТ ВСЕ ГОТОВО ДЛЯ РАСЧЕТОВ
            //Начало основного алгоритма
            //ТУТ МАКСИМИЗАЦИЯ

        }

        //Возвращает обработанную последнюю строку (Симплексную разницу). Можно поменять простое целевое значение
        public double[] Maximization(double[,] simplexTable) //Метод для решения задачи максимизации
        {
            double[] simplexDifference = new double[0];
            return simplexDifference;
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
/*
            for (int i = 0; i < constraintCount; i++)
            {
                for (int j = 0; j < varCount + constraintCount; j++)
                    Console.Write(x[i, j]);
                Console.WriteLine();
            }                
            */
