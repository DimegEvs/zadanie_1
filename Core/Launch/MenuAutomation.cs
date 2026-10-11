using System.Runtime.InteropServices;

namespace WukongBenchAuto.Core.Launch;

/// <summary>
/// Автоматизация меню бенчмарка: "нажмите любую клавишу" → "Тест быстродействия" → "Подтвердить".
/// Координаты заданы в долях клиентской области окна и не зависят от разрешения.
/// </summary>
public static partial class MenuAutomation
{
    // Кнопка "Тест быстродействия" в главном меню.
    private const double MenuItemX = 0.1115;
    private const double MenuItemY = 0.4458;

    // Кнопка "Подтвердить" в диалоге запуска теста.
    private const double ConfirmX = 0.3898;
    private const double ConfirmY = 0.5885;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint count, INPUT[] inputs, int size);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT keyboard;
        [FieldOffset(0)] public MOUSEINPUT mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx, dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private const uint InputMouse = 0;
    private const uint InputKeyboard = 1;
    private const uint KeyEventfKeyUp = 0x0002;
    private const uint MouseeventfLeftDown = 0x0002;
    private const uint MouseeventfLeftUp = 0x0004;

    /// <summary>Прогнать меню бенчмарка до запуска теста.</summary>
    public static async Task StartBenchmarkAsync(IntPtr hwnd, CancellationToken ct)
    {
        SetForegroundWindow(hwnd);
        await Task.Delay(1500, ct);

        // 1. "Нажмите любую клавишу" — Space.
        PressSpace();
        await Task.Delay(1000, ct);

        // 2. "Тест быстродействия".
        Click(hwnd, MenuItemX, MenuItemY);
        await Task.Delay(1000, ct);

        // 3. "Подтвердить".
        Click(hwnd, ConfirmX, ConfirmY);
        await Task.Delay(1000, ct);
    }

    private static void PressSpace()
    {
        var inputs = new[]
        {
            Key(0x20, 0),
            Key(0x20, KeyEventfKeyUp),
        };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT Key(ushort vk, uint flags) => new()
    {
        type = InputKeyboard,
        union = new InputUnion
        {
            keyboard = new KEYBDINPUT { wVk = vk, dwFlags = flags },
        },
    };

    private static void Click(IntPtr hwnd, double relX, double relY)
    {
        if (!GetClientRect(hwnd, out var rect))
            throw new InvalidOperationException("Не удалось получить размер окна бенчмарка.");

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        var point = new POINT
        {
            X = (int)(width * relX),
            Y = (int)(height * relY),
        };

        if (!ClientToScreen(hwnd, ref point))
            throw new InvalidOperationException("Не удалось вычислить экранные координаты.");

        SetCursor(point.X, point.Y);
        Thread.Sleep(80);
        SendClick();
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    private static void SetCursor(int x, int y) => SetCursorPos(x, y);

    private static void SendClick()
    {
        var inputs = new[]
        {
            Mouse(MouseeventfLeftDown),
            Mouse(MouseeventfLeftUp),
        };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT Mouse(uint flags) => new()
    {
        type = InputMouse,
        union = new InputUnion
        {
            mouse = new MOUSEINPUT { dwFlags = flags },
        },
    };
}
