import type { Metadata } from "next";
import { DeliveryHistory } from "../../features/messaging/DeliveryHistory";

export const metadata: Metadata = { title: "Banco KRT | Histórico de entregas" };

export default function DeliveryHistoryPage() {
  return <DeliveryHistory />;
}
