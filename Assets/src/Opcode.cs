public enum Opcode : ushort {
    push_s32  = 0,
    pop_s32   = 1,
    add_s32   = 2,
    sub_s32   = 3,
    func      = 4,
    call      = 5,
    ret       = 6,
    larg_s32  = 7,
}