// Подключаем базовые типы .NET: Exception, Random, StringComparison и т.д.
using System;

// LINQ нужен для генерации строки капчи через Enumerable.Range(...).Select(...)
using System.Linq;

// Task нужен для async/await (блокировка на 10 секунд через Task.Delay)
using System.Threading.Tasks;

// MemoryStream нужен, чтобы сохранить PNG из SkiaSharp в память и создать Bitmap Avalonia
using System.IO;

// Avalonia UI: Window, TextBox, TextBlock, StackPanel, Button, Image и т.д.
using Avalonia.Controls;

// RoutedEventArgs и события Click (OnLoginClick, OnGuestClick, OnRefreshCaptcha)
using Avalonia.Interactivity;

// Bitmap — чтобы показывать изображение капчи в <Image/>
using Avalonia.Media.Imaging;

// Entity Framework Core: Include(), FirstOrDefaultAsync(), DbContext
using Microsoft.EntityFrameworkCore;

// SkiaSharp — рисуем изображение капчи (шум, линии, символы, рамка)
using SkiaSharp;

// Пространство имен проекта (должно совпадать с x:Class="MyApp.MainWindow" в XAML)
namespace MyApp;

// partial — потому что вторая часть класса генерируется из XAML (InitializeComponent и т.п.)
public partial class MainWindow : Window
{
    // ===== ССЫЛКИ НА ЭЛЕМЕНТЫ UI (получаем через FindControl) =====

    // Поле ввода логина
    private TextBox _loginBox = null!;

    // Поле ввода пароля
    private TextBox _passwordBox = null!;

    // Текст ошибок ("Неверный логин...", "Введите логин..." и т.п.)
    private TextBlock _errorText = null!;

    // Панель, где лежит капча (по умолчанию скрыта)
    private StackPanel _captchaPanel = null!;

    // Картинка капчи (Image.Source будет Bitmap)
    private Image _captchaImage = null!;

    // Поле ввода капчи
    private TextBox _captchaBox = null!;

    // Текст блокировки ("Вход заблокирован на ... сек.")
    private TextBlock _lockText = null!;

    // Кнопка "Войти" — отключаем на 10 секунд при блокировке
    private Button _loginBtn = null!;

    // ===== СОСТОЯНИЕ АВТОРИЗАЦИИ/КАПЧИ =====

    // Счётчик неуспешных попыток входа
    // По ТЗ: после первой неудачи включаем капчу
    private int _failedCount = 0;

    // Флаг: нужно ли требовать капчу
    // false — капча не нужна
    // true — капча нужна (показываем CaptchaPanel)
    private bool _captchaRequired = false;

    // Значение капчи, которое должен ввести пользователь (4 символа)
    private string _captchaValue = "";

    // Флаг блокировки: если true, вход временно запрещён (на 10 секунд)
    private bool _isLocked = false;

    // Генератор случайных чисел: нужен для капчи (символы, шум, линии, наклон)
    private readonly Random _rnd = new();

    // ===== КОНСТРУКТОР ОКНА =====
    public MainWindow()
    {
        // Загружаем XAML-разметку (создаёт все элементы, указанные в MainWindow.axaml)
        InitializeComponent();

        // Находим элементы по x:Name из XAML
        // Если элемент не найден (ошибка имени в XAML), кидаем исключение — чтобы сразу понять проблему

        _loginBox = this.FindControl<TextBox>("LoginBox")
            ?? throw new Exception("LoginBox not found");

        _passwordBox = this.FindControl<TextBox>("PasswordBox")
            ?? throw new Exception("PasswordBox not found");

        _errorText = this.FindControl<TextBlock>("ErrorText")
            ?? throw new Exception("ErrorText not found");

        _captchaPanel = this.FindControl<StackPanel>("CaptchaPanel")
            ?? throw new Exception("CaptchaPanel not found");

        _captchaImage = this.FindControl<Image>("CaptchaImage")
            ?? throw new Exception("CaptchaImage not found");

        _captchaBox = this.FindControl<TextBox>("CaptchaBox")
            ?? throw new Exception("CaptchaBox not found");

        _lockText = this.FindControl<TextBlock>("LockText")
            ?? throw new Exception("LockText not found");

        _loginBtn = this.FindControl<Button>("LoginBtn")
            ?? throw new Exception("LoginBtn not found");
    }

    // ===== ОБРАБОТЧИК КНОПКИ "Войти" =====
    private async void OnLoginClick(object? sender, RoutedEventArgs e)
    {
        // Если сейчас активна блокировка — ничего не делаем
        if (_isLocked) return;

        // Скрываем предыдущие сообщения (ошибка и блокировка)
        _errorText.IsVisible = false;
        _lockText.IsVisible = false;

        // Берём логин/пароль из полей
        // Trim() убирает пробелы по краям логина
        var login = _loginBox.Text?.Trim() ?? "";
        var pass = _passwordBox.Text ?? "";

        // Проверяем, что логин и пароль не пустые
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(pass))
        {
            ShowError("Введите логин и пароль");
            return;
        }

        // ===== ПРОВЕРКА CAPTCHA (если она обязательна) =====
        // По ТЗ: после первой неудачи — капча появляется, и далее при входе её нужно вводить.
        if (_captchaRequired)
        {
            // Текст, который ввёл пользователь
            var inputCaptcha = (_captchaBox.Text ?? "").Trim();

            // Сравнение без учёта регистра (A == a)
            if (!string.Equals(inputCaptcha, _captchaValue, StringComparison.OrdinalIgnoreCase))
            {
                // Если капча неверная — показываем ошибку
                ShowError("Неверная CAPTCHA");

                // По ТЗ: после неудачной попытки с CAPTCHA блокируем вход на 10 секунд
                await LockLoginFor10Seconds();

                // Генерируем новую капчу (чтобы нельзя было подбирать по старой)
                RefreshCaptcha();
                return;
            }
        }

        // ===== ДОСТУП К БД / ПРОВЕРКА ЛОГИНА И ПАРОЛЯ =====
        try
        {
            // Создаём DbContext (у тебя в OnConfiguring прописан UseNpgsql)
            using var db = new DemoDbContext();

            // Ищем пользователя по логину и паролю
            // Include нужен, чтобы сразу подтянуть связанную роль (UserroleNavigation)
            var u = await db.Users
                .Include(x => x.UserroleNavigation)
                .FirstOrDefaultAsync(x => x.Userlogin == login && x.Userpassword == pass);

            // Если пользователя не нашли — авторизация неуспешна
            if (u == null)
            {
                // Увеличиваем счётчик неудачных попыток
                _failedCount++;

                // По ТЗ: после первой неудачи включаем капчу
                if (_failedCount >= 1)
                {
                    // Делаем капчу обязательной
                    _captchaRequired = true;

                    // Показываем блок капчи
                    _captchaPanel.IsVisible = true;

                    // Если капча ещё не сгенерирована — генерируем и рисуем
                    if (string.IsNullOrWhiteSpace(_captchaValue))
                        RefreshCaptcha();
                }

                // Показываем сообщение
                ShowError("Неверный логин или пароль");
                return;
            }

            // ===== ЕСЛИ АВТОРИЗАЦИЯ УСПЕШНА =====

            // Сбрасываем всё состояние (неудачи, капчу, блокировку и т.п.)
            _failedCount = 0;
            _captchaRequired = false;
            _captchaValue = "";
            _captchaPanel.IsVisible = false;
            _captchaBox.Text = "";

            // Формируем ФИО для DashboardWindow
            var fio = $"{u.Usersurname} {u.Username} {u.Userpatronymic}".Trim();

            // Берём роль из связанной таблицы Role
            var role = u.UserroleNavigation.Rolename;

            // Открываем главное окно
            var wnd = new DashboardWindow(fio, role);
            wnd.Show();

            // Закрываем окно авторизации
            Close();
        }
        catch (Exception ex)
        {
            // Любая ошибка БД/подключения/запроса — показываем пользователю
            ShowError("Ошибка БД: " + ex.Message);
        }
    }

    // ===== ОБРАБОТЧИК КНОПКИ "Гость" =====
    private void OnGuestClick(object? sender, RoutedEventArgs e)
    {
        // Открываем Dashboard в режиме гостя
        var wnd = new DashboardWindow("Гость", "гость");
        wnd.Show();

        // Закрываем окно логина
        Close();
    }

    // ===== УДОБНЫЙ МЕТОД ПОКАЗА ОШИБОК =====
    private void ShowError(string msg)
    {
        // Устанавливаем текст ошибки
        _errorText.Text = msg;

        // Делаем элемент видимым
        _errorText.IsVisible = true;
    }

    // ===================== CAPTCHA =====================

    // Обработчик кнопки "↻" — просто обновляет капчу
    private void OnRefreshCaptcha(object? sender, RoutedEventArgs e) => RefreshCaptcha();

    // Генерация новой капчи + очистка ввода + перерисовка картинки
    private void RefreshCaptcha()
    {
        // Генерим 4 символа
        _captchaValue = GenerateCaptchaText(4);

        // Очищаем поле ввода капчи
        _captchaBox.Text = "";

        // Рисуем картинку с капчей
        DrawCaptcha();
    }

    // Генерирует строку из len символов (латиница + цифры)
    private string GenerateCaptchaText(int len)
    {
        // Исключили похожие символы: 0/O, 1/I, чтобы пользователю было легче
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";

        // Создаём строку случайных символов
        return new string(
            Enumerable.Range(0, len)
                .Select(_ => alphabet[_rnd.Next(alphabet.Length)])
                .ToArray()
        );
    }

    // Рисуем картинку капчи в виде PNG и кладём в Avalonia Image.Source
    private void DrawCaptcha()
    {
        // Размер картинки капчи (совпадает с Width/Height в XAML)
        const int w = 150;
        const int h = 55;

        // Создаём растровое изображение
        using var bmp = new SKBitmap(w, h);

        // Canvas для рисования по bitmap
        using var canvas = new SKCanvas(bmp);

        // Заливаем фон белым
        canvas.Clear(SKColors.White);

        // Локальная переменная rnd (короче писать)
        var rnd = _rnd;

        // ===== ШУМ: ТОЧКИ =====
        // Серые точки, чтобы усложнить распознавание (графический шум)
        using var dotPaint = new SKPaint
        {
            Color = new SKColor(210, 210, 210),
            IsAntialias = true
        };

        // Рисуем много маленьких кружков (точек)
        for (int i = 0; i < 320; i++)
            canvas.DrawCircle(rnd.Next(w), rnd.Next(h), 1.2f, dotPaint);

        // ===== ШУМ: ЛИНИИ =====
        // Несколько случайных линий по экрану
        using var linePaint = new SKPaint
        {
            Color = new SKColor(180, 180, 180),
            StrokeWidth = 1,
            IsAntialias = true
        };

        // 8 линий шума
        for (int i = 0; i < 8; i++)
            canvas.DrawLine(
                rnd.Next(w), rnd.Next(h),
                rnd.Next(w), rnd.Next(h),
                linePaint
            );

        // ===== РИСУЕМ СИМВОЛЫ CAPTCHA =====
        // Требование ТЗ:
        // - минимум 4 символа
        // - НЕ в одной линии (по Y разные)
        // - либо перечеркнуты, либо наложены (мы делаем и наложение и перечёркивание)
        // - с графическим шумом

        using var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 28,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial")
        };

        // Базовое смещение по X для первого символа
        float xBase = 18;

        // Проходим по символам строки капчи
        for (int i = 0; i < _captchaValue.Length; i++)
        {
            var ch = _captchaValue[i].ToString();

            // X: шаг 28 + случайное смещение -> создаёт эффект наложения
            float x = xBase + i * 28 + rnd.Next(-6, 5);

            // Y: случайное смещение -> символы НЕ по одной линии
            float y = 35 + rnd.Next(-8, 10);

            // Угол поворота для каждого символа
            float angle = rnd.Next(-20, 21);

            // Сохраняем состояние canvas
            canvas.Save();

            // Переносим "точку рисования" в x,y
            canvas.Translate(x, y);

            // Поворачиваем символ
            canvas.RotateDegrees(angle);

            // Рисуем текст (символ) в точке 0,0 с учётом трансформаций
            canvas.DrawText(ch, 0, 0, textPaint);

            // Возвращаем состояние canvas обратно
            canvas.Restore();
        }

        // ===== ПЕРЕЧЁРКИВАНИЕ CAPTCHA (по ТЗ) =====
        using var strikePaint = new SKPaint
        {
            Color = new SKColor(120, 120, 120),
            StrokeWidth = 2,
            IsAntialias = true
        };

        // Две "перечёркивающие" линии сверху текста
        canvas.DrawLine(5, rnd.Next(15, 40), w - 5, rnd.Next(10, 45), strikePaint);
        canvas.DrawLine(5, rnd.Next(15, 40), w - 5, rnd.Next(10, 45), strikePaint);

        // ===== РАМКА ВОКРУГ CAPTCHA =====
        using var framePaint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 1,
            IsStroke = true
        };

        // Рамка по периметру
        canvas.DrawRect(0, 0, w - 1, h - 1, framePaint);

        // ===== КОНВЕРТАЦИЯ В AVALONIA BITMAP =====
        // Из SKBitmap делаем SKImage
        using var image = SKImage.FromBitmap(bmp);

        // Кодируем изображение в PNG в памяти
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        // Перекладываем байты PNG в MemoryStream
        using var ms = new MemoryStream(data.ToArray());

        // Создаём Avalonia Bitmap из потока и ставим в Image.Source
        _captchaImage.Source = new Bitmap(ms);
    }

    // ===================== LOCK 10s =====================

    // Блокировка входа на 10 секунд (по ТЗ)
    private async Task LockLoginFor10Seconds()
    {
        // Ставим флаг, чтобы OnLoginClick сразу выходил
        _isLocked = true;

        // Отключаем кнопку и поля ввода, чтобы пользователь не мог нажимать/вводить
        // (и чтобы текст блокировки при этом нормально показывался)
        _loginBtn.IsEnabled = false;
        _loginBox.IsEnabled = false;
        _passwordBox.IsEnabled = false;
        _captchaBox.IsEnabled = false;

        // Показываем текст блокировки
        _lockText.IsVisible = true;

        // Обратный отсчёт 10...1
        for (int sec = 10; sec >= 1; sec--)
        {
            // Обновляем текст
            _lockText.Text = $"Вход заблокирован на {sec} сек.";

            // Ждём 1 секунду
            await Task.Delay(1000);
        }

        // Скрываем текст блокировки
        _lockText.IsVisible = false;

        // Включаем обратно управление
        _loginBtn.IsEnabled = true;
        _loginBox.IsEnabled = true;
        _passwordBox.IsEnabled = true;
        _captchaBox.IsEnabled = true;

        // Снимаем блокировку
        _isLocked = false;
    }
}
