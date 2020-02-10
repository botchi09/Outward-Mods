using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataminer_2
{
    public class EnemyHolder : IEqualityComparer<EnemyHolder>
    {
        private void test()
        {
            var x = new EnemyHolder();
            var y = new EnemyHolder();

            if (x.Equals(y))
            {
                
            }
        }

        bool IEqualityComparer<EnemyHolder>.Equals(EnemyHolder x, EnemyHolder y)
        {
            // todo
            throw new NotImplementedException();
        }

        int IEqualityComparer<EnemyHolder>.GetHashCode(EnemyHolder obj)
        {
            throw new NotImplementedException();
        }
    }
}
