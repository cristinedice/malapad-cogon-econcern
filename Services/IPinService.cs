namespace MalapadCogonEConcern.Services;

public interface IPinService
{
    (string Hash, string Salt) HashPin(string pin);
    bool VerifyPin(string pin, string hash, string salt);
}
