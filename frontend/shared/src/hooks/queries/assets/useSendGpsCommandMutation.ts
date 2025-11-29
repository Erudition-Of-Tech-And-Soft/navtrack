import { useQueryClient } from "@tanstack/react-query";
import { useAssetsSendCommand } from "../../../api";

export function useSendGpsCommandMutation() {
  const queryClient = useQueryClient();

  const mutation = useAssetsSendCommand({
    mutation: {
      onSuccess: () => {
        // Optionally invalidate queries if needed
        // For now, we just return success
        return Promise.resolve();
      }
    }
  });

  return mutation;
}
