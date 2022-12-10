using System;

namespace _1_Calculator
{
    public class Calculator
    {
        public enum CalculatorOperationType
        {
            Addition,
            Substraction,
            Dividing,
            Multiplying,
            None
        };

        static Calculator()
        {
            InputBuffer = "0";
            OutputBuffer = string.Empty;
            InputRestriction = -1;

            isFirstNumValueSet = false;
            isBlocked = false;
            doesTheInputContainResOrPrevNum = false;
            doesTheInputContainConstantValue = false;
            isUnaryOperation = false;
            doesTheLastOutputContainsParentheses = false;
            isRadians = false;

            firstNumValue = 0;
            secondNumValue = 0;
            unaryBuffer = string.Empty;
            lastComputation = string.Empty;

            nextOperationType = CalculatorOperationType.None;
            prevOperationType = CalculatorOperationType.None;

            DecimalSeparator = Convert.ToChar(System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat
                .NumberDecimalSeparator);
        }

        //fields
        private static string inputBuffer;
        private static string outputBuffer;
        private static string unaryBuffer;
        private static string lastComputation;
        private static int inputRestriction;

        private static bool isFirstNumValueSet;
        private static bool doesTheInputContainResOrPrevNum;
        private static bool doesTheInputContainConstantValue;
        private static bool isBlocked;
        private static bool isUnaryOperation;
        private static bool doesTheLastOutputContainsParentheses;
        private static bool isRadians;

        private static double firstNumValue;
        private static double secondNumValue;

        public static event EventHandler InputBufferChanged;
        public static event EventHandler OutputBufferChanged;
        public static event EventHandler ComputationEnded; //!

        private static CalculatorOperationType nextOperationType;
        private static CalculatorOperationType prevOperationType;

        private const double Pi = Math.PI;
        private static readonly char DecimalSeparator;

        //properties
        public static string InputBuffer
        {
            get => inputBuffer;
            private set
            {
                if (doesTheInputContainResOrPrevNum || doesTheInputContainConstantValue)
                {
                    if (inputBuffer != string.Empty) value = value.Remove(0, inputBuffer.Length);
                    if (value != string.Empty && value[0] == DecimalSeparator) value = value.Insert(0, "0");
                    doesTheInputContainResOrPrevNum = false;
                    doesTheInputContainConstantValue = false;
                }

                if (value.Length > 1 && value[0] == '0')
                {
                    if (value[1] != DecimalSeparator)
                        value = value.Substring(1);
                }

                inputBuffer = value;
                OnInputBufferChanged(EventArgs.Empty);
            }
        }

        public static string OutputBuffer
        {
            get
            {
                if (outputBuffer.Length >= 40) return "..." + outputBuffer.Substring(outputBuffer.Length - 40, 40);
                return outputBuffer;
            }
            private set
            {
                outputBuffer = value;
                OnOutputBufferChanged(EventArgs.Empty);
            }
        }

        //No restriction = -1
        private static int InputRestriction
        {
            get => inputRestriction;
            set
            {
                if (value < -1) value = Math.Abs(value);
                inputRestriction = value;
            }
        }

        public static void EnterNumber(int num)
        {
            if (IsInputAllowed())
            {
                if (isUnaryOperation)
                {
                    EraseUnaryOperation();
                    InputBuffer = num.ToString();
                }
                else InputBuffer += num.ToString();
            }
        }

        public static void EnterDot()
        {
            if (IsInputAllowed() && (!InputBuffer.Contains(DecimalSeparator.ToString()) ||
                                     doesTheInputContainResOrPrevNum || doesTheInputContainConstantValue))
            {
                if (isUnaryOperation)
                {
                    EraseUnaryOperation();
                    InputBuffer = "0" + DecimalSeparator.ToString();
                }
                else InputBuffer += DecimalSeparator;
            }
        }

        public static void EnterPi()
        {
            if (IsInputAllowed())
            {
                if (isUnaryOperation) EraseUnaryOperation();
                doesTheInputContainResOrPrevNum = true;
                if (InputRestriction == -1 || Pi.ToString().Length <= InputRestriction) InputBuffer += Pi.ToString();
                else InputBuffer += Pi.ToString().Substring(0, InputRestriction - 1);
                doesTheInputContainConstantValue = true;
            }
        }

        public static void EraseLast()
        {
            if (!doesTheInputContainResOrPrevNum && !isBlocked && !doesTheInputContainConstantValue &&
                !isUnaryOperation)
            {
                InputBuffer = InputBuffer.Remove(InputBuffer.Length - 1);
                if (InputBuffer.Length == 0 || (InputBuffer.Length == 1 && InputBuffer[0] == '-')) InputBuffer = "0";
            }
        }

        public static void ClearEntry()
        {
            doesTheInputContainConstantValue = false;
            doesTheInputContainResOrPrevNum = false;
            if (isUnaryOperation) EraseUnaryOperation();
            InputBuffer = "0";
            if (isBlocked)
            {
                OutputBuffer = string.Empty;
                unaryBuffer = string.Empty;
                isBlocked = false;
            }
        }

        public static void Clear()
        {
            doesTheInputContainResOrPrevNum = false;
            ClearOutputBuffer();

            isBlocked = false;
            InputBuffer = "0";
        }

        public static void ChangeMeasureMode()
        {
            isRadians = !isRadians;
        }

        public static void Equal()
        {
            if (!isBlocked)
            {
                if (doesTheInputContainResOrPrevNum ||
                    (nextOperationType == CalculatorOperationType.None && isUnaryOperation))
                {
                    doesTheInputContainResOrPrevNum = true;

                    if (OutputBuffer != string.Empty)
                    {
                        if (isUnaryOperation)
                        {
                            lastComputation = outputBuffer + " = " + InputBuffer;
                            ComputationEnded?.Invoke(null, EventArgs.Empty);
                        }
                        else if (prevOperationType != CalculatorOperationType.None ||
                                 outputBuffer[outputBuffer.Length - 4] == ')')
                        {
                            lastComputation = outputBuffer.Remove(outputBuffer.Length - 2, 2) + "= " + InputBuffer;
                            ComputationEnded?.Invoke(null, EventArgs.Empty);
                        }
                    }

                    ClearOutputBuffer();
                }
                else if ((prevOperationType != CalculatorOperationType.None ||
                          (prevOperationType == CalculatorOperationType.None &&
                           nextOperationType != CalculatorOperationType.None)))
                {
                    if (Double.TryParse(InputBuffer, out secondNumValue))
                    {
                        string secondNum = InputBuffer;
                        ExecuteOperationImpl(nextOperationType);
                        if (!isUnaryOperation) lastComputation = outputBuffer + secondNum + " = " + InputBuffer;
                        else lastComputation = outputBuffer + " = " + InputBuffer;

                        ComputationEnded?.Invoke(null, EventArgs.Empty);
                        ClearOutputBuffer();
                    }
                }
            }
        }

        public static void Factorial()
        {
            if (!isBlocked && !InputBuffer.Contains(DecimalSeparator.ToString()))
            {
                if (Int32.TryParse(InputBuffer, out var num))
                {
                    int res = num;
                    if (num > 0)
                    {
                        for (int i = num - 1; i > 0; i--)
                        {
                            res *= i;
                        }
                    }
                    else if (num == 0)
                    {
                        res = 1;
                    }
                    else
                    {
                        InvokeError();
                        return;
                    }

                    PrintUnaryOperation(num, "factorial");
                    InputBuffer = res.ToString();
                }
            }
        }

        public static void ChangeSign()
        {
            if (!isBlocked)
            {
                if (Double.TryParse(InputBuffer, out var num))
                {
                    PrintUnaryOperation(num, "negate");
                    InputBuffer = (num * (-1)).ToString();
                }
            }
        }

        public static void SquareRoot()
        {
            if (!isBlocked)
            {
                if (Double.TryParse(InputBuffer, out var num))
                {
                    if (num >= 0)
                    {
                        PrintUnaryOperation(num, "sqrt");
                        InputBuffer = Math.Sqrt(num).ToString();
                    }
                    else
                    {
                        InvokeError();
                    }
                }
            }
        }

        public static void ToTheSquare()
        {
            if (!isBlocked)
            {
                if (Double.TryParse(InputBuffer, out var num))
                {
                    PrintUnaryOperation(num, "sqr");
                    InputBuffer = Math.Pow(num, 2).ToString();
                }
            }
        }

        public static void Sin()
        {
            if (!isBlocked)
            {
                if (Double.TryParse(InputBuffer, out var num))
                {
                    PrintUnaryOperation(num, "sin");
                    if (!isRadians) num = Pi * (num / 180);
                    if (doesTheInputContainConstantValue) doesTheInputContainConstantValue = false;
                    InputBuffer = Math.Round(Math.Sin(num), 4).ToString();
                }
            }
        }

        public static void Cos()
        {
            if (!isBlocked)
            {
                if (Double.TryParse(InputBuffer, out var num))
                {
                    PrintUnaryOperation(num, "cos");
                    if (!isRadians) num = Pi * (num / 180);
                    if (doesTheInputContainConstantValue) doesTheInputContainConstantValue = false;
                    InputBuffer = Math.Round(Math.Cos(num), 4).ToString();
                }
            }
        }

        public static void Tan()
        {
            if (!isBlocked)
            {
                if (!isBlocked)
                {
                    if (Double.TryParse(InputBuffer, out var num))
                    {
                        PrintUnaryOperation(num, "tan");
                        if (!isRadians)
                        {
                            num = Pi * (num / 180);
                        }

                        num = Math.Tan(num);
                        if (num > 1)
                        {
                            InvokeError();
                            return;
                        }

                        if (doesTheInputContainConstantValue) doesTheInputContainConstantValue = false;
                        InputBuffer = Math.Round(num, 4).ToString();
                    }
                }
            }
        }

        private static void AddNextOperation(in CalculatorOperationType operation, char sign)
        {
            if (Double.TryParse(InputBuffer, out var num))
            {
                if (!isFirstNumValueSet)
                {
                    firstNumValue = num;
                    isFirstNumValueSet = true;
                }
                else secondNumValue = num;

                if (nextOperationType != CalculatorOperationType.None &&
                    (!doesTheInputContainResOrPrevNum || doesTheInputContainConstantValue))
                {
                    ExecuteOperationImpl(nextOperationType);
                    if (isBlocked) return;

                    string outputValueStr;
                    if (!isUnaryOperation)
                    {
                        outputValueStr = secondNumValue.ToString();
                    }
                    else
                    {
                        outputValueStr = string.Empty;
                        isUnaryOperation = false;
                        unaryBuffer = string.Empty;
                    }

                    if ((operation == CalculatorOperationType.Multiplying ||
                         operation == CalculatorOperationType.Dividing)
                        && (prevOperationType == CalculatorOperationType.Addition ||
                            prevOperationType == CalculatorOperationType.Substraction)
                        && !doesTheLastOutputContainsParentheses)
                    {
                        OutputBuffer = "(" + outputBuffer + outputValueStr + ")" + ' ' + sign + ' ';
                        doesTheLastOutputContainsParentheses = true;
                    }
                    else OutputBuffer = outputBuffer + outputValueStr + ' ' + sign + ' ';
                }
                else if (!doesTheInputContainResOrPrevNum || doesTheInputContainConstantValue)
                {
                    if (!isUnaryOperation) OutputBuffer = outputBuffer + firstNumValue.ToString() + ' ' + sign + ' ';
                    else
                    {
                        OutputBuffer = outputBuffer + " " + sign + " ";
                        isUnaryOperation = false;
                        unaryBuffer = string.Empty;
                    }

                    if (InputBuffer[InputBuffer.Length - 1] == DecimalSeparator || (InputBuffer.Length > 1 &&
                            InputBuffer[InputBuffer.Length - 2]
                            == DecimalSeparator &&
                            InputBuffer[InputBuffer.Length - 1] == '0')) InputBuffer = firstNumValue.ToString();
                }
                else
                {
                    if (prevOperationType != CalculatorOperationType.None
                        && (operation == CalculatorOperationType.Substraction ||
                            operation == CalculatorOperationType.Addition)
                        && doesTheLastOutputContainsParentheses)
                    {
                        OutputBuffer =
                            (outputBuffer.Remove(outputBuffer.Length - 4, 4) + ' ' + sign + ' ').Remove(0, 1);
                        doesTheLastOutputContainsParentheses = false;
                    }
                    else if (prevOperationType != CalculatorOperationType.None
                             && (operation == CalculatorOperationType.Multiplying ||
                                 operation == CalculatorOperationType.Dividing)
                             && !doesTheLastOutputContainsParentheses)
                    {
                        OutputBuffer = "(" + outputBuffer.Remove(outputBuffer.Length - 3, 3) + ")" + ' ' + sign + ' ';
                        doesTheLastOutputContainsParentheses = true;
                    }
                    else
                    {
                        if (OutputBuffer == string.Empty && doesTheInputContainResOrPrevNum)
                            OutputBuffer = firstNumValue.ToString() + " " + sign + " ";
                        else OutputBuffer = outputBuffer.Remove(outputBuffer.Length - 2, 2) + sign + ' ';
                    }
                }

                nextOperationType = operation;
                doesTheInputContainResOrPrevNum = true;
                doesTheInputContainConstantValue = false;
            }
        }

        private static void ExecuteOperationImpl(CalculatorOperationType op)
        {
            switch (op)
            {
                case CalculatorOperationType.Addition:
                    firstNumValue += secondNumValue;
                    break;
                case CalculatorOperationType.Substraction:
                    firstNumValue -= secondNumValue;
                    break;
                case CalculatorOperationType.Dividing:
                    if (secondNumValue == 0)
                    {
                        InvokeError();
                        return;
                    }
                    else
                    {
                        firstNumValue /= secondNumValue;
                    }

                    break;
                case CalculatorOperationType.Multiplying:
                    firstNumValue *= secondNumValue;
                    break;
            }

            prevOperationType = nextOperationType;
            nextOperationType = CalculatorOperationType.None;
            unaryBuffer = string.Empty;
            doesTheInputContainConstantValue = false;
            InputBuffer = firstNumValue.ToString();
            doesTheInputContainResOrPrevNum = true;
            doesTheLastOutputContainsParentheses = false;
        }

        public static void ExecuteOperation(CalculatorOperationType op)
        {
            if (op != CalculatorOperationType.None && !isBlocked)
            {
                switch (op)
                {
                    case CalculatorOperationType.Addition:
                        AddNextOperation(CalculatorOperationType.Addition, '+');
                        break;
                    case CalculatorOperationType.Substraction:
                        AddNextOperation(CalculatorOperationType.Substraction, '-');
                        break;
                    case CalculatorOperationType.Dividing:
                        AddNextOperation(CalculatorOperationType.Dividing, '/');
                        break;
                    case CalculatorOperationType.Multiplying:
                        AddNextOperation(CalculatorOperationType.Multiplying, '*');
                        break;
                }
            }
        }

        private static bool IsInputAllowed()
        {
            return (!isBlocked && (doesTheInputContainResOrPrevNum ||
                                   (InputRestriction == -1 || InputBuffer.Length + 1 <= InputRestriction) ||
                                   isUnaryOperation));
        }

        private static void EraseUnaryOperation()
        {
            if (OutputBuffer != string.Empty && nextOperationType != CalculatorOperationType.None)
            {
                if (unaryBuffer != string.Empty)
                {
                    OutputBuffer = outputBuffer.Substring(0, outputBuffer.Length - (unaryBuffer.Length + 1)) + ' ';
                }
            }
            else OutputBuffer = string.Empty;

            isUnaryOperation = false;
            unaryBuffer = string.Empty;
        }

        private static void PrintUnaryOperation(double num, string functionName)
        {
            if (unaryBuffer != string.Empty)
            {
                string tempBuffer = unaryBuffer;
                EraseUnaryOperation();
                unaryBuffer = functionName + "(" + tempBuffer + ")";
            }
            else unaryBuffer = functionName + "(" + num.ToString() + ")";

            doesTheInputContainResOrPrevNum = false;
            isUnaryOperation = true;
            OutputBuffer = outputBuffer + unaryBuffer;
        }

        private static void InvokeError()
        {
            prevOperationType = CalculatorOperationType.None;
            nextOperationType = CalculatorOperationType.None;
            doesTheInputContainResOrPrevNum = false;
            isFirstNumValueSet = false;
            isBlocked = true;
            InputBuffer = "Error";
        }

        private static void ClearOutputBuffer()
        {
            isFirstNumValueSet = false;
            doesTheInputContainConstantValue = false;
            isUnaryOperation = false;
            OutputBuffer = string.Empty;
            unaryBuffer = string.Empty;
            doesTheLastOutputContainsParentheses = false;
            nextOperationType = CalculatorOperationType.None;
            prevOperationType = CalculatorOperationType.None;
        }

        private static void OnInputBufferChanged(EventArgs e) => InputBufferChanged?.Invoke(null, e);
        private static void OnOutputBufferChanged(EventArgs e) => OutputBufferChanged?.Invoke(null, e);
    }
}