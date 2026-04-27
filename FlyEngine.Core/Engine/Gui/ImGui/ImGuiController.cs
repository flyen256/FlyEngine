using System.Drawing;
using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace FlyEngine.Core.Gui.ImGui;

public class ImGuiController : IDisposable
{
  private GL _gl;
  private IView _view;
  private IInputContext _input;
  private bool _frameBegun;
  private readonly List<char> _pressedChars = new List<char>();
  private IKeyboard _keyboard;
  private int _attribLocationTex;
  private int _attribLocationProjMtx;
  private int _attribLocationVtxPos;
  private int _attribLocationVtxUV;
  private int _attribLocationVtxColor;
  private uint _vboHandle;
  private uint _elementsHandle;
  private uint _vertexArrayObject;
  private ImGuiTexture _fontTexture;
  private ImGuiShader _shader;
  private int _windowWidth;
  private int _windowHeight;
  public IntPtr Context;
  private Vector2D<int> _minSize;
  public ImFontPtr ArialFont;

  /// <summary>Constructs a new ImGuiController.</summary>
  public ImGuiController(GL gl, IView view, IInputContext input, Vector2D<int> minSize)
    : this(gl, view, input, minSize, new ImGuiFontConfig?(), (Action) null)
  {
  }

  /// <summary>
  /// Constructs a new ImGuiController with font configuration.
  /// </summary>
  public ImGuiController(GL gl, IView view, IInputContext input, Vector2D<int> minSize, ImGuiFontConfig imGuiFontConfig)
    : this(gl, view, input, minSize, new ImGuiFontConfig?(imGuiFontConfig))
  {
  }

  /// <summary>
  /// Constructs a new ImGuiController with an onConfigureIO Action.
  /// </summary>
  public ImGuiController(GL gl, IView view, IInputContext input, Vector2D<int> minSize, Action onConfigureIO)
    : this(gl, view, input, minSize, new ImGuiFontConfig?(), onConfigureIO)
  {
  }

  /// <summary>
  /// Constructs a new ImGuiController with font configuration and onConfigure Action.
  /// </summary>
  public unsafe ImGuiController(
    GL gl,
    IView view,
    IInputContext input,
    Vector2D<int> minSize,
    ImGuiFontConfig? imGuiFontConfig = null,
    Action onConfigureIO = null)
  {
    _minSize = minSize;
    Init(gl, view, input);
    ImGuiIOPtr io = ImGuiNET.ImGui.GetIO();
    if (imGuiFontConfig.HasValue)
    {
      Func<ImGuiIOPtr, IntPtr> getGlyphRange = imGuiFontConfig.Value.GetGlyphRange;
      IntPtr glyph_ranges = getGlyphRange != null ? getGlyphRange(io) : IntPtr.Zero;
      io.Fonts.AddFontFromFileTTF(imGuiFontConfig.Value.FontPath, (float) imGuiFontConfig.Value.FontSize, (ImFontConfigPtr) (ImFontConfig*) null, glyph_ranges);
    }
    var currentDir = Directory.GetCurrentDirectory();
    var resourcesDir = new DirectoryInfo(Path.Combine(currentDir, "Resources"));
    var fontInfo = new FileInfo(Path.Combine(resourcesDir.FullName, "Fonts", "DejaVuSans.ttf"));
    ArialFont = io.Fonts.AddFontFromFileTTF(fontInfo.FullName, 16.0f, null,
      io.Fonts.GetGlyphRangesCyrillic());
    if (onConfigureIO != null)
      onConfigureIO();
    io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
    CreateDeviceResources();
    SetPerFrameImGuiData(0.016666668f);
    BeginFrame();
  }

  public void MakeCurrent() => ImGuiNET.ImGui.SetCurrentContext(Context);

  private void Init(GL gl, IView view, IInputContext input)
  {
    _gl = gl;
    _view = view;
    _input = input;
    _windowWidth = view.Size.X;
    _windowHeight = view.Size.Y;
    Context = ImGuiNET.ImGui.CreateContext();
    ImGuiNET.ImGui.SetCurrentContext(Context);
    ImGuiNET.ImGui.StyleColorsDark();
  }

  private uint fontTexture;

  private void BeginFrame()
  {
    ImGuiNET.ImGui.NewFrame();
    _frameBegun = true;
    _keyboard = _input.Keyboards[0];
    _view.Resize += new Action<Vector2D<int>>(WindowResized);
    // ISSUE: reference to a compiler-generated field
    // ISSUE: reference to a compiler-generated field
    _keyboard.KeyDown += OnKeyDown;
    // ISSUE: reference to a compiler-generated field
    // ISSUE: reference to a compiler-generated field
    _keyboard.KeyUp += OnKeyUp;
    _keyboard.KeyChar += new Action<IKeyboard, char>(OnKeyChar);
  }

  /// <summary>Delegate to receive keyboard key down events.</summary>
  /// <param name="keyboard">The keyboard context generating the event.</param>
  /// <param name="keycode">The native keycode of the pressed key.</param>
  /// <param name="scancode">The native scancode of the pressed key.</param>
  private static void OnKeyDown(IKeyboard keyboard, Key keycode, int scancode)
  {
    OnKeyEvent(keyboard, keycode, scancode, true);
  }

  /// <summary>Delegate to receive keyboard key up events.</summary>
  /// <param name="keyboard">The keyboard context generating the event.</param>
  /// <param name="keycode">The native keycode of the released key.</param>
  /// <param name="scancode">The native scancode of the released key.</param>
  private static void OnKeyUp(IKeyboard keyboard, Key keycode, int scancode)
  {
    OnKeyEvent(keyboard, keycode, scancode, false);
  }

  /// <summary>Delegate to receive keyboard key events.</summary>
  /// <param name="keyboard">The keyboard context generating the event.</param>
  /// <param name="keycode">The native keycode of the key generating the event.</param>
  /// <param name="scancode">The native scancode of the key generating the event.</param>
  /// <param name="down">True if the event is a key down event, otherwise False</param>
  private static void OnKeyEvent(IKeyboard keyboard, Key keycode, int scancode, bool down)
  {
    ImGuiIOPtr io = ImGuiNET.ImGui.GetIO();
    ImGuiKey imGuiKey = TranslateInputKeyToImGuiKey(keycode);
    io.AddKeyEvent(imGuiKey, down);
    io.SetKeyEventNativeData(imGuiKey, (int) keycode, scancode);
  }

  private void OnKeyChar(IKeyboard arg1, char arg2) => _pressedChars.Add(arg2);

  private void WindowResized(Vector2D<int> size)
  {
    _windowWidth = size.X;
    _windowHeight = size.Y;
  }

  /// <summary>
  /// Renders the ImGui draw list data.
  /// This method requires a <see cref="!:GraphicsDevice" /> because it may create new DeviceBuffers if the size of vertex
  /// or index data has increased beyond the capacity of the existing buffers.
  /// A <see cref="!:CommandList" /> is needed to submit drawing and resource update commands.
  /// </summary>
  public void Render()
  {
    if (!_frameBegun)
      return;
    IntPtr currentContext = ImGuiNET.ImGui.GetCurrentContext();
    if (currentContext != Context)
      ImGuiNET.ImGui.SetCurrentContext(Context);
    _frameBegun = false;
    ImGuiNET.ImGui.Render();
    RenderImDrawData(ImGuiNET.ImGui.GetDrawData());
    if (!(currentContext != Context))
      return;
    ImGuiNET.ImGui.SetCurrentContext(currentContext);
  }

  /// <summary>Updates ImGui input and IO configuration state.</summary>
  public void Update(float deltaSeconds)
  {
    IntPtr currentContext = ImGuiNET.ImGui.GetCurrentContext();
    if (currentContext != Context)
      ImGuiNET.ImGui.SetCurrentContext(Context);
    if (_frameBegun)
      ImGuiNET.ImGui.Render();
    SetPerFrameImGuiData(deltaSeconds);
    UpdateImGuiInput();
    _frameBegun = true;
    ImGuiNET.ImGui.NewFrame();
    if (!(currentContext != Context))
      return;
    ImGuiNET.ImGui.SetCurrentContext(currentContext);
  }

  /// <summary>
  /// Sets per-frame data based on the associated window.
  /// This is called by Update(float).
  /// </summary>
  private void SetPerFrameImGuiData(float deltaSeconds)
  {
    ImGuiIOPtr io = ImGuiNET.ImGui.GetIO();
    io.DisplaySize = new Vector2((float) System.Math.Max(_windowWidth, _minSize.X), (float) System.Math.Max(_windowHeight, _minSize.Y));
    if (_windowWidth > 0 && _windowHeight > 0)
      io.DisplayFramebufferScale = new Vector2(
        (float) (_view.FramebufferSize.X / System.Math.Max(_windowWidth, _minSize.X)),
        (float) (_view.FramebufferSize.Y / System.Math.Max(_windowHeight, _minSize.Y)));
    io.DeltaTime = deltaSeconds;
  }

  private void UpdateImGuiInput()
  {
    ImGuiIOPtr io = ImGuiNET.ImGui.GetIO();
    using (MouseState mouseState = _input.Mice[0].CaptureState())
    {
      io.MouseDown[0] = mouseState.IsButtonPressed(MouseButton.Left);
      io.MouseDown[1] = mouseState.IsButtonPressed(MouseButton.Right);
      io.MouseDown[2] = mouseState.IsButtonPressed(MouseButton.Middle);
      Point point = new Point((int) mouseState.Position.X, (int) mouseState.Position.Y);
      io.MousePos = new Vector2((float) point.X, (float) point.Y);
      ScrollWheel scrollWheel = mouseState.GetScrollWheels()[0];
      io.MouseWheel = scrollWheel.Y;
      io.MouseWheelH = scrollWheel.X;
      foreach (char pressedChar in _pressedChars)
        io.AddInputCharacter((uint) pressedChar);
      _pressedChars.Clear();
      io.KeyCtrl = _keyboard.IsKeyPressed(Key.ControlLeft) || _keyboard.IsKeyPressed(Key.ControlRight);
      io.KeyAlt = _keyboard.IsKeyPressed(Key.AltLeft) || _keyboard.IsKeyPressed(Key.AltRight);
      io.KeyShift = _keyboard.IsKeyPressed(Key.ShiftLeft) || _keyboard.IsKeyPressed(Key.ShiftRight);
      io.KeySuper = _keyboard.IsKeyPressed(Key.SuperLeft) || _keyboard.IsKeyPressed(Key.SuperRight);
    }
  }

  internal void PressChar(char keyChar) => _pressedChars.Add(keyChar);

  /// <summary>Translates a Silk.NET.Input.Key to an ImGuiKey.</summary>
  /// <param name="key">The Silk.NET.Input.Key to translate.</param>
  /// <returns>The corresponding ImGuiKey.</returns>
  private static ImGuiKey TranslateInputKeyToImGuiKey(Key key)
  {
    ImGuiKey imGuiKey;
    switch (key)
    {
      case Key.Space:
        imGuiKey = ImGuiKey.Space;
        break;
      case Key.Apostrophe:
        imGuiKey = ImGuiKey.Apostrophe;
        break;
      case Key.Comma:
        imGuiKey = ImGuiKey.Comma;
        break;
      case Key.Minus:
        imGuiKey = ImGuiKey.Minus;
        break;
      case Key.Period:
        imGuiKey = ImGuiKey.Period;
        break;
      case Key.Slash:
        imGuiKey = ImGuiKey.Slash;
        break;
      case Key.Number0:
        imGuiKey = ImGuiKey._0;
        break;
      case Key.Number1:
        imGuiKey = ImGuiKey._1;
        break;
      case Key.Number2:
        imGuiKey = ImGuiKey._2;
        break;
      case Key.Number3:
        imGuiKey = ImGuiKey._3;
        break;
      case Key.Number4:
        imGuiKey = ImGuiKey._4;
        break;
      case Key.Number5:
        imGuiKey = ImGuiKey._5;
        break;
      case Key.Number6:
        imGuiKey = ImGuiKey._6;
        break;
      case Key.Number7:
        imGuiKey = ImGuiKey._7;
        break;
      case Key.Number8:
        imGuiKey = ImGuiKey._8;
        break;
      case Key.Number9:
        imGuiKey = ImGuiKey._9;
        break;
      case Key.Semicolon:
        imGuiKey = ImGuiKey.Semicolon;
        break;
      case Key.Equal:
        imGuiKey = ImGuiKey.Equal;
        break;
      case Key.A:
        imGuiKey = ImGuiKey.A;
        break;
      case Key.B:
        imGuiKey = ImGuiKey.B;
        break;
      case Key.C:
        imGuiKey = ImGuiKey.C;
        break;
      case Key.D:
        imGuiKey = ImGuiKey.D;
        break;
      case Key.E:
        imGuiKey = ImGuiKey.E;
        break;
      case Key.F:
        imGuiKey = ImGuiKey.F;
        break;
      case Key.G:
        imGuiKey = ImGuiKey.G;
        break;
      case Key.H:
        imGuiKey = ImGuiKey.H;
        break;
      case Key.I:
        imGuiKey = ImGuiKey.I;
        break;
      case Key.J:
        imGuiKey = ImGuiKey.J;
        break;
      case Key.K:
        imGuiKey = ImGuiKey.K;
        break;
      case Key.L:
        imGuiKey = ImGuiKey.L;
        break;
      case Key.M:
        imGuiKey = ImGuiKey.M;
        break;
      case Key.N:
        imGuiKey = ImGuiKey.N;
        break;
      case Key.O:
        imGuiKey = ImGuiKey.O;
        break;
      case Key.P:
        imGuiKey = ImGuiKey.P;
        break;
      case Key.Q:
        imGuiKey = ImGuiKey.Q;
        break;
      case Key.R:
        imGuiKey = ImGuiKey.R;
        break;
      case Key.S:
        imGuiKey = ImGuiKey.S;
        break;
      case Key.T:
        imGuiKey = ImGuiKey.T;
        break;
      case Key.U:
        imGuiKey = ImGuiKey.U;
        break;
      case Key.V:
        imGuiKey = ImGuiKey.V;
        break;
      case Key.W:
        imGuiKey = ImGuiKey.W;
        break;
      case Key.X:
        imGuiKey = ImGuiKey.X;
        break;
      case Key.Y:
        imGuiKey = ImGuiKey.Y;
        break;
      case Key.Z:
        imGuiKey = ImGuiKey.Z;
        break;
      case Key.LeftBracket:
        imGuiKey = ImGuiKey.LeftBracket;
        break;
      case Key.BackSlash:
        imGuiKey = ImGuiKey.Backslash;
        break;
      case Key.RightBracket:
        imGuiKey = ImGuiKey.RightBracket;
        break;
      case Key.GraveAccent:
        imGuiKey = ImGuiKey.GraveAccent;
        break;
      case Key.Escape:
        imGuiKey = ImGuiKey.Escape;
        break;
      case Key.Enter:
        imGuiKey = ImGuiKey.Enter;
        break;
      case Key.Tab:
        imGuiKey = ImGuiKey.NamedKey_BEGIN;
        break;
      case Key.Backspace:
        imGuiKey = ImGuiKey.Backspace;
        break;
      case Key.Insert:
        imGuiKey = ImGuiKey.Insert;
        break;
      case Key.Delete:
        imGuiKey = ImGuiKey.Delete;
        break;
      case Key.Right:
        imGuiKey = ImGuiKey.RightArrow;
        break;
      case Key.Left:
        imGuiKey = ImGuiKey.LeftArrow;
        break;
      case Key.Down:
        imGuiKey = ImGuiKey.DownArrow;
        break;
      case Key.Up:
        imGuiKey = ImGuiKey.UpArrow;
        break;
      case Key.PageUp:
        imGuiKey = ImGuiKey.PageUp;
        break;
      case Key.PageDown:
        imGuiKey = ImGuiKey.PageDown;
        break;
      case Key.Home:
        imGuiKey = ImGuiKey.Home;
        break;
      case Key.End:
        imGuiKey = ImGuiKey.End;
        break;
      case Key.CapsLock:
        imGuiKey = ImGuiKey.CapsLock;
        break;
      case Key.ScrollLock:
        imGuiKey = ImGuiKey.ScrollLock;
        break;
      case Key.NumLock:
        imGuiKey = ImGuiKey.NumLock;
        break;
      case Key.PrintScreen:
        imGuiKey = ImGuiKey.PrintScreen;
        break;
      case Key.Pause:
        imGuiKey = ImGuiKey.Pause;
        break;
      case Key.F1:
        imGuiKey = ImGuiKey.F1;
        break;
      case Key.F2:
        imGuiKey = ImGuiKey.F2;
        break;
      case Key.F3:
        imGuiKey = ImGuiKey.F3;
        break;
      case Key.F4:
        imGuiKey = ImGuiKey.F4;
        break;
      case Key.F5:
        imGuiKey = ImGuiKey.F5;
        break;
      case Key.F6:
        imGuiKey = ImGuiKey.F6;
        break;
      case Key.F7:
        imGuiKey = ImGuiKey.F7;
        break;
      case Key.F8:
        imGuiKey = ImGuiKey.F8;
        break;
      case Key.F9:
        imGuiKey = ImGuiKey.F9;
        break;
      case Key.F10:
        imGuiKey = ImGuiKey.F10;
        break;
      case Key.F11:
        imGuiKey = ImGuiKey.F11;
        break;
      case Key.F12:
        imGuiKey = ImGuiKey.F12;
        break;
      case Key.F13:
        imGuiKey = ImGuiKey.F13;
        break;
      case Key.F14:
        imGuiKey = ImGuiKey.F14;
        break;
      case Key.F15:
        imGuiKey = ImGuiKey.F15;
        break;
      case Key.F16:
        imGuiKey = ImGuiKey.F16;
        break;
      case Key.F17:
        imGuiKey = ImGuiKey.F17;
        break;
      case Key.F18:
        imGuiKey = ImGuiKey.F18;
        break;
      case Key.F19:
        imGuiKey = ImGuiKey.F19;
        break;
      case Key.F20:
        imGuiKey = ImGuiKey.F20;
        break;
      case Key.F21:
        imGuiKey = ImGuiKey.F21;
        break;
      case Key.F22:
        imGuiKey = ImGuiKey.F22;
        break;
      case Key.F23:
        imGuiKey = ImGuiKey.F23;
        break;
      case Key.F24:
        imGuiKey = ImGuiKey.F24;
        break;
      case Key.Keypad0:
        imGuiKey = ImGuiKey.Keypad0;
        break;
      case Key.Keypad1:
        imGuiKey = ImGuiKey.Keypad1;
        break;
      case Key.Keypad2:
        imGuiKey = ImGuiKey.Keypad2;
        break;
      case Key.Keypad3:
        imGuiKey = ImGuiKey.Keypad3;
        break;
      case Key.Keypad4:
        imGuiKey = ImGuiKey.Keypad4;
        break;
      case Key.Keypad5:
        imGuiKey = ImGuiKey.Keypad5;
        break;
      case Key.Keypad6:
        imGuiKey = ImGuiKey.Keypad6;
        break;
      case Key.Keypad7:
        imGuiKey = ImGuiKey.Keypad7;
        break;
      case Key.Keypad8:
        imGuiKey = ImGuiKey.Keypad8;
        break;
      case Key.Keypad9:
        imGuiKey = ImGuiKey.Keypad9;
        break;
      case Key.KeypadDecimal:
        imGuiKey = ImGuiKey.KeypadDecimal;
        break;
      case Key.KeypadDivide:
        imGuiKey = ImGuiKey.KeypadDivide;
        break;
      case Key.KeypadMultiply:
        imGuiKey = ImGuiKey.KeypadMultiply;
        break;
      case Key.KeypadSubtract:
        imGuiKey = ImGuiKey.KeypadSubtract;
        break;
      case Key.KeypadAdd:
        imGuiKey = ImGuiKey.KeypadAdd;
        break;
      case Key.KeypadEnter:
        imGuiKey = ImGuiKey.KeypadEnter;
        break;
      case Key.KeypadEqual:
        imGuiKey = ImGuiKey.KeypadEqual;
        break;
      case Key.ShiftLeft:
        imGuiKey = ImGuiKey.LeftShift;
        break;
      case Key.ControlLeft:
        imGuiKey = ImGuiKey.LeftCtrl;
        break;
      case Key.AltLeft:
        imGuiKey = ImGuiKey.LeftAlt;
        break;
      case Key.SuperLeft:
        imGuiKey = ImGuiKey.LeftSuper;
        break;
      case Key.ShiftRight:
        imGuiKey = ImGuiKey.RightShift;
        break;
      case Key.ControlRight:
        imGuiKey = ImGuiKey.RightCtrl;
        break;
      case Key.AltRight:
        imGuiKey = ImGuiKey.RightAlt;
        break;
      case Key.SuperRight:
        imGuiKey = ImGuiKey.RightSuper;
        break;
      case Key.Menu:
        imGuiKey = ImGuiKey.Menu;
        break;
      default:
        imGuiKey = ImGuiKey.None;
        break;
    }
    return imGuiKey;
  }

  private unsafe void SetupRenderState(
    ImDrawDataPtr drawDataPtr,
    int framebufferWidth,
    int framebufferHeight)
  {
    _gl.Enable(GLEnum.Blend);
    _gl.BlendEquation(GLEnum.FuncAdd);
    _gl.BlendFuncSeparate(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha, GLEnum.True, GLEnum.OneMinusSrcAlpha);
    _gl.Disable(GLEnum.CullFace);
    _gl.Disable(GLEnum.DepthTest);
    _gl.Disable(GLEnum.StencilTest);
    _gl.Enable(GLEnum.ScissorTest);
    _gl.Disable(GLEnum.PrimitiveRestart);
    _gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
    float x = drawDataPtr.DisplayPos.X;
    float num1 = drawDataPtr.DisplayPos.X + drawDataPtr.DisplaySize.X;
    float y = drawDataPtr.DisplayPos.Y;
    float num2 = drawDataPtr.DisplayPos.Y + drawDataPtr.DisplaySize.Y;
    Span<float> span = stackalloc float[16 /*0x10*/]
    {
      (float) (2.0 / ((double) num1 - (double) x)),
      0.0f,
      0.0f,
      0.0f,
      0.0f,
      (float) (2.0 / ((double) y - (double) num2)),
      0.0f,
      0.0f,
      0.0f,
      0.0f,
      -1f,
      0.0f,
      (float) (((double) num1 + (double) x) / ((double) x - (double) num1)),
      (float) (((double) y + (double) num2) / ((double) num2 - (double) y)),
      0.0f,
      1f
    };
    _shader.UseShader();
    _gl.Uniform1(_attribLocationTex, 0);
    _gl.UniformMatrix4(_attribLocationProjMtx, 1U, false, (ReadOnlySpan<float>) span);
    _gl.BindSampler(0U, 0U);
    _vertexArrayObject = _gl.GenVertexArray();
    _gl.BindVertexArray(_vertexArrayObject);
    _gl.BindBuffer(GLEnum.ArrayBuffer, _vboHandle);
    _gl.BindBuffer(GLEnum.ElementArrayBuffer, _elementsHandle);
    _gl.EnableVertexAttribArray((uint) _attribLocationVtxPos);
    _gl.EnableVertexAttribArray((uint) _attribLocationVtxUV);
    _gl.EnableVertexAttribArray((uint) _attribLocationVtxColor);
    _gl.VertexAttribPointer((uint) _attribLocationVtxPos, 2, GLEnum.Float, false, (uint) sizeof (ImDrawVert), (void*) null);
    _gl.VertexAttribPointer((uint) _attribLocationVtxUV, 2, GLEnum.Float, false, (uint) sizeof (ImDrawVert), (void*) new IntPtr(8));
    _gl.VertexAttribPointer((uint) _attribLocationVtxColor, 4, GLEnum.UnsignedByte, true, (uint) sizeof (ImDrawVert), (void*) new IntPtr(16));
  }

  private unsafe void RenderImDrawData(ImDrawDataPtr drawDataPtr)
  {
    int framebufferWidth = (int) ((double) drawDataPtr.DisplaySize.X * (double) drawDataPtr.FramebufferScale.X);
    int framebufferHeight = (int) ((double) drawDataPtr.DisplaySize.Y * (double) drawDataPtr.FramebufferScale.Y);
    if (framebufferWidth <= 0 || framebufferHeight <= 0)
      return;
    int data1;
    _gl.GetInteger(GLEnum.ActiveTexture, out data1);
    _gl.ActiveTexture(GLEnum.Texture0);
    int data2;
    _gl.GetInteger(GLEnum.CurrentProgram, out data2);
    int data3;
    _gl.GetInteger(GLEnum.TextureBinding2D, out data3);
    int data4;
    _gl.GetInteger(GLEnum.SamplerBinding, out data4);
    int data5;
    _gl.GetInteger(GLEnum.ArrayBufferBinding, out data5);
    int data6;
    _gl.GetInteger(GLEnum.VertexArrayBinding, out data6);
    Span<int> data7 = stackalloc int[2];
    _gl.GetInteger(GLEnum.PolygonMode, data7);
    Span<int> data8 = stackalloc int[4];
    _gl.GetInteger(GLEnum.ScissorBox, data8);
    int data9;
    _gl.GetInteger(GLEnum.BlendSrcRgb, out data9);
    int data10;
    _gl.GetInteger(GLEnum.BlendDstRgb, out data10);
    int data11;
    _gl.GetInteger(GLEnum.BlendSrcAlpha, out data11);
    int data12;
    _gl.GetInteger(GLEnum.BlendDstAlpha, out data12);
    int data13;
    _gl.GetInteger(GLEnum.BlendEquation, out data13);
    int data14;
    _gl.GetInteger(GLEnum.BlendEquationAlpha, out data14);
    bool flag1 = _gl.IsEnabled(GLEnum.Blend);
    bool flag2 = _gl.IsEnabled(GLEnum.CullFace);
    bool flag3 = _gl.IsEnabled(GLEnum.DepthTest);
    bool flag4 = _gl.IsEnabled(GLEnum.StencilTest);
    bool flag5 = _gl.IsEnabled(GLEnum.ScissorTest);
    bool flag6 = _gl.IsEnabled(GLEnum.PrimitiveRestart);
    SetupRenderState(drawDataPtr, framebufferWidth, framebufferHeight);
    Vector2 vector2_1 = drawDataPtr.DisplayPos;
    Vector2 vector2_2 = drawDataPtr.FramebufferScale;
    for (int index1 = 0; index1 < drawDataPtr.CmdListsCount; ++index1)
    {
      ImDrawListPtr imDrawListPtr = drawDataPtr.CmdLists[index1];
      _gl.BufferData(GLEnum.ArrayBuffer, (UIntPtr) (imDrawListPtr.VtxBuffer.Size * sizeof (ImDrawVert)), (void*) imDrawListPtr.VtxBuffer.Data, GLEnum.StreamDraw);
      _gl.BufferData(GLEnum.ElementArrayBuffer, (UIntPtr) (imDrawListPtr.IdxBuffer.Size * 2), (void*) imDrawListPtr.IdxBuffer.Data, GLEnum.StreamDraw);
      for (int index2 = 0; index2 < imDrawListPtr.CmdBuffer.Size; ++index2)
      {
        ImDrawCmdPtr imDrawCmdPtr = imDrawListPtr.CmdBuffer[index2];
        if (imDrawCmdPtr.UserCallback != IntPtr.Zero)
          throw new NotImplementedException();
        Vector4 vector4;
        vector4.X = (imDrawCmdPtr.ClipRect.X - vector2_1.X) * vector2_2.X;
        vector4.Y = (imDrawCmdPtr.ClipRect.Y - vector2_1.Y) * vector2_2.Y;
        vector4.Z = (imDrawCmdPtr.ClipRect.Z - vector2_1.X) * vector2_2.X;
        vector4.W = (imDrawCmdPtr.ClipRect.W - vector2_1.Y) * vector2_2.Y;
        if ((double) vector4.X < (double) framebufferWidth && (double) vector4.Y < (double) framebufferHeight && (double) vector4.Z >= 0.0 && (double) vector4.W >= 0.0)
        {
          _gl.Scissor((int) vector4.X, (int) ((double) framebufferHeight - (double) vector4.W), (uint) ((double) vector4.Z - (double) vector4.X), (uint) ((double) vector4.W - (double) vector4.Y));
          _gl.BindTexture(GLEnum.Texture2D, (uint) (int) imDrawCmdPtr.TextureId);
          _gl.DrawElementsBaseVertex(GLEnum.Triangles, imDrawCmdPtr.ElemCount, GLEnum.UnsignedShort, (void*) (imDrawCmdPtr.IdxOffset * 2U), (int) imDrawCmdPtr.VtxOffset);
        }
      }
    }
    _gl.DeleteVertexArray(_vertexArrayObject);
    _vertexArrayObject = 0U;
    _gl.UseProgram((uint) data2);
    _gl.BindTexture(GLEnum.Texture2D, (uint) data3);
    _gl.BindSampler(0U, (uint) data4);
    _gl.ActiveTexture((GLEnum) data1);
    _gl.BindVertexArray((uint) data6);
    _gl.BindBuffer(GLEnum.ArrayBuffer, (uint) data5);
    _gl.BlendEquationSeparate((GLEnum) data13, (GLEnum) data14);
    _gl.BlendFuncSeparate((GLEnum) data9, (GLEnum) data10, (GLEnum) data11, (GLEnum) data12);
    if (flag1)
      _gl.Enable(GLEnum.Blend);
    else
      _gl.Disable(GLEnum.Blend);
    if (flag2)
      _gl.Enable(GLEnum.CullFace);
    else
      _gl.Disable(GLEnum.CullFace);
    if (flag3)
      _gl.Enable(GLEnum.DepthTest);
    else
      _gl.Disable(GLEnum.DepthTest);
    if (flag4)
      _gl.Enable(GLEnum.StencilTest);
    else
      _gl.Disable(GLEnum.StencilTest);
    if (flag5)
      _gl.Enable(GLEnum.ScissorTest);
    else
      _gl.Disable(GLEnum.ScissorTest);
    if (flag6)
      _gl.Enable(GLEnum.PrimitiveRestart);
    else
      _gl.Disable(GLEnum.PrimitiveRestart);
    _gl.PolygonMode(GLEnum.FrontAndBack, (GLEnum) data7[0]);
    _gl.Scissor(data8[0], data8[1], (uint) data8[2], (uint) data8[3]);
  }

  private void CreateDeviceResources()
  {
    int data1;
    _gl.GetInteger(GLEnum.TextureBinding2D, out data1);
    int data2;
    _gl.GetInteger(GLEnum.ArrayBufferBinding, out data2);
    int data3;
    _gl.GetInteger(GLEnum.VertexArrayBinding, out data3);
    _shader = new ImGuiShader(_gl, "#version 330\n        layout (location = 0) in vec2 Position;\n        layout (location = 1) in vec2 UV;\n        layout (location = 2) in vec4 Color;\n        uniform mat4 ProjMtx;\n        out vec2 Frag_UV;\n        out vec4 Frag_Color;\n        void main()\n        {\n            Frag_UV = UV;\n            Frag_Color = Color;\n            gl_Position = ProjMtx * vec4(Position.xy,0,1);\n        }", "#version 330\n        in vec2 Frag_UV;\n        in vec4 Frag_Color;\n        uniform sampler2D Texture;\n        layout (location = 0) out vec4 Out_Color;\n        void main()\n        {\n            Out_Color = Frag_Color * texture(Texture, Frag_UV.st);\n        }");
    _attribLocationTex = _shader.GetUniformLocation("Texture");
    _attribLocationProjMtx = _shader.GetUniformLocation("ProjMtx");
    _attribLocationVtxPos = _shader.GetAttribLocation("Position");
    _attribLocationVtxUV = _shader.GetAttribLocation("UV");
    _attribLocationVtxColor = _shader.GetAttribLocation("Color");
    _vboHandle = _gl.GenBuffer();
    _elementsHandle = _gl.GenBuffer();
    RecreateFontDeviceTexture();
    _gl.BindTexture(GLEnum.Texture2D, (uint) data1);
    _gl.BindBuffer(GLEnum.ArrayBuffer, (uint) data2);
    _gl.BindVertexArray((uint) data3);
  }

  /// <summary>Creates the texture used to render text.</summary>
  public void RecreateFontDeviceTexture()
  {
    ImGuiIOPtr io = ImGuiNET.ImGui.GetIO();
    IntPtr out_pixels;
    int out_width;
    int out_height;
    io.Fonts.GetTexDataAsRGBA32(out out_pixels, out out_width, out out_height, out int _);
    int data;
    _gl.GetInteger(GLEnum.TextureBinding2D, out data);
    _fontTexture = new ImGuiTexture(_gl, out_width, out_height, out_pixels);
    _fontTexture.Bind();
    _fontTexture.SetMagFilter(TextureMagFilter.Linear);
    _fontTexture.SetMinFilter(TextureMinFilter.Linear);
    io.Fonts.SetTexID((IntPtr) (long) _fontTexture.GlTexture);
    _gl.BindTexture(GLEnum.Texture2D, (uint) data);
  }

  /// <summary>Frees all graphics resources used by the renderer.</summary>
  public void Dispose()
  {
    _view.Resize -= new Action<Vector2D<int>>(WindowResized);
    _keyboard.KeyChar -= new Action<IKeyboard, char>(OnKeyChar);
    _gl.DeleteBuffer(_vboHandle);
    _gl.DeleteBuffer(_elementsHandle);
    _gl.DeleteVertexArray(_vertexArrayObject);
    _fontTexture.Dispose();
    _shader.Dispose();
    ImGuiNET.ImGui.DestroyContext(Context);
  }
}