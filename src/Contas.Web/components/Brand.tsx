import Image from "next/image";

export function Brand() {
  return (
    <div className="brand">
      <Image src="/brand/mark.png" width={40} height={40} alt="" unoptimized />
      <div><strong>Banco KRT</strong><span>Gestão de contas</span></div>
    </div>
  );
}
