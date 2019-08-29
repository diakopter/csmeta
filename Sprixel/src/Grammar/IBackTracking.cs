using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sprixel {
    public interface IBackTracking {
        Transition Done { get; set; }
        Transition Notd { get; set; }
        Transition Back { get; set; }
        Transition Init { get; set; }
    }
}
