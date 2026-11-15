import Image from "next/image";

export function Brand() {
  return <div className="brand"><Image className="krt-logo" src="/brand/logo.png" width={80} height={45} alt="" unoptimized /><strong>Banco KRT</strong></div>;
}
