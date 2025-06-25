using LMApp.Models;
using LMApp.Models.Account;
using LMApp.Models.Transactions;
using System.Net.Http.Headers;
using System.Text.Json;

public class TransactionDto
{
    public long id { get; set; }

    public string date { get; set; }
    public string payee { get; set; }
    public decimal amount { get; set; }
    public string currency { get; set; }

    public decimal to_base { get; set; }

    public long? category_id { get; set; }

    public string category_name { get; set; }

    public long? category_group_id { get; set; }

    public string category_group_name { get; set; }

    public bool is_income { get; set; }

    public bool exclude_from_budget { get; set; }


    public bool exclude_from_totals { get; set; }
    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public string status { get; set; }

    public bool is_pending { get; set; }


    public string notes { get; set; }

    public string original_name { get; set; }

    public long? recurring_id { get; set; }

    public string recurring_payee { get; set; }

    public string recurring_description { get; set; }

    public string recurring_cadence { get; set; }

    public string recurring_type { get; set; }

    public decimal? recurring_amount { get; set; }

    public string recurring_currency { get; set; }

    public long? parent_id { get; set; }

    public bool has_children { get; set; }

    public long? group_id { get; set; }

    public bool is_group { get; set; }

    public long? asset_id { get; set; }

    public string asset_institution_name { get; set; }

    public string asset_name { get; set; }

    public string asset_display_name { get; set; }

    public string asset_status { get; set; }

    public long? plaid_account_id { get; set; }

    public string plaid_account_name { get; set; }

    public string plaid_account_mask { get; set; }

    public string institution_name { get; set; }

    public string plaid_account_display_name { get; set; }
    public string plaid_metadata { get; set; }
    public string source { get; set; }
    public string display_name { get; set; }
    public string display_notes { get; set; }
    public string account_display_name { get; set; }
    public Tag[] tags { get; set; }
    public TransactionChildDto[] children { get; set; }
    public string external_id { get; set; }

    public JsonElement? error { get; set; }

    public IEnumerable<string> GetErrors()
    {
        if (error == null)
        {
            yield break;
        }
        else if (error.Value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in error.Value.EnumerateArray())
            {
                yield return item.GetString();
            }
        }
        else
        {
            yield return error.Value.GetString();
        }
    }


    public string AccountUid => TransactionsService.GetAccountUid(asset_id, plaid_account_id);

    public TransactionForEditDto GetEditDto()
    {
        return new TransactionForEditDto
        {
            id = id,
            date = date,
            amount = amount,
            category_id = category_id,
            payee = payee,
            currency = currency,
            asset_id = asset_id,
            plaid_account_id = plaid_account_id,
            recurring_id = recurring_id,
            notes = notes,
            status = status,
            external_id = external_id,
            tags = tags.Select(t => t.name).ToList(),
        };
    }




}