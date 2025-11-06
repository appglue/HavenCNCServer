;------------------------------------------------------------------------------
; Filename: setup_key_defines.cnc
; Description: Setup Key Defines to make M222 Key Presses easier to read.
; Notes:
; Requires: CNC12 V5.18+
; Revision Date: 30 Apr 2024
; Please see TB300 or the following link for tips on writing custom macros.
; https://www.centroidcnc.com/centroid_diy/downloads/acorn_documentation/centroid_cnc_macro_programming.pdf
;------------------------------------------------------------------------------
; Usage: G65 "#400\system\setup_key_defines.cnc"
;------------------------------------------------------------------------------
; Parameters:
;
; System Variables:
;
; PLC Variables:
;
; User Variables:
;
;------------------------------------------------------------------------------
IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

;Extended Keycode Value
DEFINE <EXTENDED_KEYCODE> 256

;Backspace, Tab, Enter, Space Keys
; Shift, Ctrl, Alt Versions are same values, should not use the Shift, Ctrl, or Alt
; versions with the exception of Ctrl Enter.
DEFINE <KB_BACKSPACE>       [8]
DEFINE <KB_SHIFT_BACKSPACE> [8]
DEFINE <KB_CTRL_BACKSPACE>  [8]
DEFINE <KB_ALT_BACKSPACE>   [8]
DEFINE <KB_TAB>             [9]
DEFINE <KB_ENTER>           [13]
DEFINE <KB_SHIFT_ENTER>     [13]
DEFINE <KB_ALT_ENTER>       [13]
DEFINE <KB_SPACE>           [32]
DEFINE <KB_SHIFT_SPACE>     [32]
DEFINE <KB_CTRL_SPACE>      [32]
DEFINE <KB_ALT_SPACE>       [32]

;Shift 0-9 Keys
; Numpad Keys are same Values
DEFINE <KB_SHIFT_0>         [41]
DEFINE <KB_SHIFT_1>         [33]
DEFINE <KB_SHIFT_2>         [64]
DEFINE <KB_SHIFT_3>         [35]
DEFINE <KB_SHIFT_4>         [36]
DEFINE <KB_SHIFT_5>         [37]
DEFINE <KB_SHIFT_6>         [94]
DEFINE <KB_SHIFT_7>         [38]
DEFINE <KB_SHIFT_8>         [42] ; (*)
DEFINE <KB_SHIFT_9>         [40]

;0-9 Keys
; Numpad Keys are same Values
DEFINE <KB_0>               [48]
DEFINE <KB_1>               [49]
DEFINE <KB_2>               [50]
DEFINE <KB_3>               [51]
DEFINE <KB_4>               [52]
DEFINE <KB_5>               [53]
DEFINE <KB_6>               [54]
DEFINE <KB_7>               [55]
DEFINE <KB_8>               [56]
DEFINE <KB_9>               [57]

;Ctrl 0-9 Keys
; Numpad Keys are same Values
; Same Values as 0-9 Keys, should not use.
DEFINE <KB_CTRL_0>          [48]
DEFINE <KB_CTRL_1>          [49]
DEFINE <KB_CTRL_2>          [50]
DEFINE <KB_CTRL_3>          [51]
DEFINE <KB_CTRL_4>          [52]
DEFINE <KB_CTRL_5>          [53]
DEFINE <KB_CTRL_6>          [54]
DEFINE <KB_CTRL_7>          [55]
DEFINE <KB_CTRL_8>          [56]
DEFINE <KB_CTRL_9>          [57]

;Shift A-Z Keys
DEFINE <KB_SHIFT_A>         [65]
DEFINE <KB_SHIFT_B>         [66]
DEFINE <KB_SHIFT_C>         [67]
DEFINE <KB_SHIFT_D>         [68]
DEFINE <KB_SHIFT_E>         [69]
DEFINE <KB_SHIFT_F>         [70]
DEFINE <KB_SHIFT_G>         [71]
DEFINE <KB_SHIFT_H>         [72]
DEFINE <KB_SHIFT_I>         [73]
DEFINE <KB_SHIFT_J>         [74]
DEFINE <KB_SHIFT_K>         [75]
DEFINE <KB_SHIFT_L>         [76]
DEFINE <KB_SHIFT_M>         [77]
DEFINE <KB_SHIFT_N>         [78]
DEFINE <KB_SHIFT_O>         [79]
DEFINE <KB_SHIFT_P>         [80]
DEFINE <KB_SHIFT_Q>         [81]
DEFINE <KB_SHIFT_R>         [82]
DEFINE <KB_SHIFT_S>         [83]
DEFINE <KB_SHIFT_T>         [84]
DEFINE <KB_SHIFT_U>         [85]
DEFINE <KB_SHIFT_V>         [86]
DEFINE <KB_SHIFT_W>         [87]
DEFINE <KB_SHIFT_X>         [88]
DEFINE <KB_SHIFT_Y>         [89]
DEFINE <KB_SHIFT_Z>         [90]

;A-Z Keys
DEFINE <KB_A>               [97]
DEFINE <KB_B>               [98]
DEFINE <KB_C>               [99]
DEFINE <KB_D>               [100]
DEFINE <KB_E>               [101]
DEFINE <KB_F>               [102]
DEFINE <KB_G>               [103]
DEFINE <KB_H>               [104]
DEFINE <KB_I>               [105]
DEFINE <KB_J>               [106]
DEFINE <KB_K>               [107]
DEFINE <KB_L>               [108]
DEFINE <KB_M>               [109]
DEFINE <KB_N>               [110]
DEFINE <KB_O>               [111]
DEFINE <KB_P>               [112]
DEFINE <KB_Q>               [113]
DEFINE <KB_R>               [114]
DEFINE <KB_S>               [115]
DEFINE <KB_T>               [116]
DEFINE <KB_U>               [117]
DEFINE <KB_V>               [118]
DEFINE <KB_W>               [119]
DEFINE <KB_X>               [120]
DEFINE <KB_Y>               [121]
DEFINE <KB_Z>               [122]

;Alt A-Z Keys
DEFINE <KB_ALT_Q>           [16 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_W>           [17 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_E>           [18 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_R>           [19 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_T>           [20 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_Y>           [21 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_U>           [22 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_I>           [23 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_O>           [24 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_P>           [25 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_A>           [30 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_S>           [31 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_D>           [32 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_F>           [33 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_G>           [34 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_H>           [35 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_J>           [36 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_K>           [37 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_L>           [38 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_Z>           [44 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_X>           [45 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_C>           [46 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_V>           [47 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_B>           [48 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_N>           [49 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_M>           [50 + <EXTENDED_KEYCODE>]

;F Keys
DEFINE <KB_F1>              [59 + <EXTENDED_KEYCODE>]
DEFINE <KB_F2>              [60 + <EXTENDED_KEYCODE>]
DEFINE <KB_F3>              [61 + <EXTENDED_KEYCODE>]
DEFINE <KB_F4>              [62 + <EXTENDED_KEYCODE>]
DEFINE <KB_F5>              [63 + <EXTENDED_KEYCODE>]
DEFINE <KB_F6>              [64 + <EXTENDED_KEYCODE>]
DEFINE <KB_F7>              [65 + <EXTENDED_KEYCODE>]
DEFINE <KB_F8>              [66 + <EXTENDED_KEYCODE>]
DEFINE <KB_F9>              [67 + <EXTENDED_KEYCODE>]
DEFINE <KB_F10>             [68 + <EXTENDED_KEYCODE>]

;Navigation Keys
DEFINE <KB_HOME>            [71 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_HOME>      [71 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_HOME>        [71 + <EXTENDED_KEYCODE>]
DEFINE <KB_UP>              [72 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_UP>        [72 + <EXTENDED_KEYCODE>]
DEFINE <KB_CTRL_UP>         [72 + <EXTENDED_KEYCODE>]
DEFINE <KB_PAGE_UP>         [73 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_PAGE_UP>   [73 + <EXTENDED_KEYCODE>]
DEFINE <KB_CTRL_PAGE_UP>    [73 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_PAGE_UP>     [73 + <EXTENDED_KEYCODE>]
DEFINE <KB_LEFT>            [75 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_LEFT>      [75 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_LEFT>        [75 + <EXTENDED_KEYCODE>]
DEFINE <KB_RIGHT>           [77 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_RIGHT>     [77 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_RIGHT>       [77 + <EXTENDED_KEYCODE>]
DEFINE <KB_END>             [79 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_END>       [79 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_END>         [79 + <EXTENDED_KEYCODE>]
DEFINE <KB_DOWN>            [80 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_DOWN>      [80 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_DOWN>        [80 + <EXTENDED_KEYCODE>]
DEFINE <KB_PAGE_DOWN>       [81 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_PAGE_DOWN> [81 + <EXTENDED_KEYCODE>]
DEFINE <KB_CTRL_PAGE_DOWN>  [81 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_PAGE_DOWN>   [81 + <EXTENDED_KEYCODE>]
DEFINE <KB_INSERT>          [82 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_INSERT>    [82 + <EXTENDED_KEYCODE>]
DEFINE <KB_CTRL_INSERT>     [82 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_INSERT>      [82 + <EXTENDED_KEYCODE>]
DEFINE <KB_DELETE>          [83 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_DELETE>    [83 + <EXTENDED_KEYCODE>]
DEFINE <KB_CTRL_DELETE>     [83 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_DELETE>      [83 + <EXTENDED_KEYCODE>]

;Shift F Keys
DEFINE <KB_SHIFT_F1>        [84 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_F2>        [85 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_F3>        [86 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_F4>        [87 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_F5>        [88 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_F6>        [89 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_F7>        [90 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_F8>        [91 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_F9>        [92 + <EXTENDED_KEYCODE>]
DEFINE <KB_SHIFT_F10>       [93 + <EXTENDED_KEYCODE>]

;Ctrl F Keys
DEFINE <KB_CTRL_F1>         [94 + <EXTENDED_KEYCODE>]
DEFINE <KB_CTRL_F2>         [95 + <EXTENDED_KEYCODE>]
DEFINE <KB_CTRL_F3>         [96 + <EXTENDED_KEYCODE>]
DEFINE <KB_CTRL_F4>         [97 + <EXTENDED_KEYCODE>]
DEFINE <KB_CTRL_F5>         [98 + <EXTENDED_KEYCODE>]
DEFINE <KB_CTRL_F6>         [99 + <EXTENDED_KEYCODE>]
DEFINE <KB_CTRL_F7>         [100 + <EXTENDED_KEYCODE>]
DEFINE <KB_CTRL_F8>         [101 + <EXTENDED_KEYCODE>]
DEFINE <KB_CTRL_F9>         [102 + <EXTENDED_KEYCODE>]
DEFINE <KB_CTRL_F10>        [103 + <EXTENDED_KEYCODE>]

;Alt F Keys
DEFINE <KB_ALT_F1>          [104 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_F2>          [105 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_F3>          [106 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_F4>          [107 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_F5>          [108 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_F6>          [109 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_F7>          [110 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_F8>          [111 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_F9>          [112 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_F10>         [113 + <EXTENDED_KEYCODE>]

;Ctrl Navigation Keys
DEFINE <KB_CTRL_LEFT>       [115 + <EXTENDED_KEYCODE>]
DEFINE <KB_CTRL_RIGHT>      [116 + <EXTENDED_KEYCODE>]
DEFINE <KB_CTRL_END>        [117 + <EXTENDED_KEYCODE>]
DEFINE <KB_CTRL_HOME>       [119 + <EXTENDED_KEYCODE>]

;Alt 0-9 Keys
; Numpad Keys are same Values
DEFINE <KB_ALT_0>           [129 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_1>           [120 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_2>           [121 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_3>           [122 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_4>           [123 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_5>           [124 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_6>           [125 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_7>           [126 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_8>           [127 + <EXTENDED_KEYCODE>]
DEFINE <KB_ALT_9>           [128 + <EXTENDED_KEYCODE>]

;Ctrl Control Keys
DEFINE <KB_CTRL_ENTER>      [175 + <EXTENDED_KEYCODE>]

;Ctrl Navigation Keys
DEFINE <KB_CTRL_UP>         [179 + <EXTENDED_KEYCODE>]
DEFINE <KB_CTRL_DOWN>       [180 + <EXTENDED_KEYCODE>]

N1000                            ;End of Macro