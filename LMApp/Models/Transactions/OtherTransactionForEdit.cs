using LMApp.Models.Context;
using System.Reflection.Metadata.Ecma335;

namespace LMApp.Models.Transactions
{
    public class OtherTransactionForEdit : BaseTransactionForEdit
    {

        public OtherTransactionForEdit()
        {
        }

        override public TransactionType TranType => TransactionType.Other;

        public decimal Amount { get; set; }

        public bool IsCredit { get; set; }

        public long? CategoryId { get; set; }

        public string Payee { get; set; }


        public string Currency { get; set; }

        public string AccountUid { get; set; }

        public string Notes { get; set; }
        public List<string> Tags { get; set; }

        public override bool HasChanges => false;


        override public TransactionForEditDto[] GetUpdateDtos(SettingsService settingsService)
        {
            throw new NotImplementedException();
        }

        public TransactionForEditDto GetUpdateDto()
        {
            throw new NotImplementedException();
        }

        public override long? GroupTransactionId => null;

        public override IdAndAmount[] ChildTransactionIds => Array.Empty<IdAndAmount>();

        public override bool TypeCanBeChanged => false;


        public override bool HasEditChanges => false;

        public override string Name => $"Other";


        public override TransactionForInsertDto[] GetInsertDtos(SettingsService settingsService)
        {
            throw new NotImplementedException();
        }

        override public void TrimAll()
        {
            Payee = Payee?.Trim();
            Notes = Notes?.Trim();
        }

        public override void UpdateWith(BaseTransactionForEdit other, SettingsService service)
        {
        }

    }
}
