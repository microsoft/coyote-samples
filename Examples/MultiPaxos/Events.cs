using Microsoft.Coyote;

namespace Coyote.Examples.MultiPaxos
{
    #region Events

    class local : Event { }
    class success : Event { }
    class goPropose : Event { }
    class response : Event { }

    #endregion
}
