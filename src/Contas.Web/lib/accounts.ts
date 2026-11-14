export type Status = "active" | "inactive";
export type Account = { id: string; holderName: string; cpf: string; status: Status; version: number };
export type Draft = { holderName: string; cpf: string; status: Status };

export function formatCpf(value: string) {
  return value.replace(/\D/g, "").slice(0, 11).replace(/^(\d{3})(\d)/, "$1.$2").replace(/^(\d{3}\.\d{3})(\d)/, "$1.$2").replace(/(\.\d{3})(\d)/, "$1-$2");
}

export function isValidCpf(value: string) {
  const digits = value.replace(/\D/g, "");
  if (!/^\d{11}$/.test(digits) || /^(\d)\1+$/.test(digits)) return false;
  return [9, 10].every(length => {
    const sum = Array.from(digits.slice(0, length)).reduce((total, digit, i) => total + Number(digit) * (length + 1 - i), 0);
    const remainder = (sum * 10) % 11;
    return (remainder === 10 ? 0 : remainder) === Number(digits[length]);
  });
}
