import { faTerminal } from "@fortawesome/free-solid-svg-icons";
import { useState, useCallback } from "react";
import { FormattedMessage } from "react-intl";
import { ModalActions } from "../../ui/modal/ModalActions";
import { ModalContainer } from "../../ui/modal/ModalContainer";
import { ModalContent } from "../../ui/modal/ModalContent";
import { ModalIcon } from "../../ui/modal/ModalIcon";
import { Button } from "../../ui/button/Button";
import { Modal } from "../../ui/modal/Modal";
import { ModalBody } from "../../ui/modal/ModalBody";
import { Asset } from "@navtrack/shared/api/model";
import { useSendGpsCommandMutation } from "@navtrack/shared/hooks/queries/assets/useSendGpsCommandMutation";
import { useCurrentOrganization } from "@navtrack/shared/hooks/current/useCurrentOrganization";

type GpsCommandsModalProps = {
  asset: Asset;
  open: boolean;
  close: () => void;
};

const GPS_COMMANDS = [
  { id: "CutFuel", labelId: "gps.commands.cut-fuel", color: "danger" as const },
  {
    id: "RestoreFuel",
    labelId: "gps.commands.restore-fuel",
    color: "success" as const
  },
  { id: "Fortify", labelId: "gps.commands.fortify", color: "primary" as const },
  { id: "Withdraw", labelId: "gps.commands.withdraw", color: "white" as const },
  {
    id: "QueryLocation",
    labelId: "gps.commands.query-location",
    color: "primary" as const
  },
  { id: "Restart", labelId: "gps.commands.restart", color: "white" as const },
  {
    id: "RestoreFactory",
    labelId: "gps.commands.restore-factory",
    color: "danger" as const
  },
  {
    id: "StopRecordings",
    labelId: "gps.commands.stop-recordings",
    color: "white" as const
  }
];

export function GpsCommandsModal(props: GpsCommandsModalProps) {
  const currentOrganization = useCurrentOrganization();
  const sendCommandMutation = useSendGpsCommandMutation();
  const [result, setResult] = useState<{
    success: boolean;
    message: string;
  } | null>(null);

  const handleSendCommand = useCallback(
    async (commandType: string) => {
      setResult(null);
      try {
        const response = await sendCommandMutation.mutateAsync({
          organizationId: currentOrganization.id!,
          assetId: props.asset.id,
          data: { commandType }
        });

        setResult({
          success: response.data.success,
          message: response.data.message
        });
      } catch (error) {
        setResult({
          success: false,
          message: "Error al enviar comando. Intente nuevamente."
        });
      }
    },
    [
      sendCommandMutation,
      currentOrganization.id,
      props.asset.id
    ]
  );

  return (
    <Modal open={props.open} close={props.close} className="w-full max-w-md">
      <ModalContainer>
        <ModalContent>
          <ModalIcon icon={faTerminal} />
          <ModalBody>
            <div className="text-md font-medium">
              <FormattedMessage id="gps.commands.title" />
            </div>
            <div className="mt-2 text-sm text-gray-600">
              <FormattedMessage
                id="gps.commands.subtitle"
                values={{ asset: props.asset.name }}
              />
            </div>
            {result && (
              <div
                className={`mt-4 rounded p-3 text-sm ${
                  result.success
                    ? "bg-green-100 text-green-800"
                    : "bg-red-100 text-red-800"
                }`}>
                {result.message}
              </div>
            )}
            <div className="mt-4 grid grid-cols-2 gap-2">
              {GPS_COMMANDS.map((cmd) => (
                <Button
                  key={cmd.id}
                  color={cmd.color}
                  size="sm"
                  onClick={() => handleSendCommand(cmd.id)}
                  isLoading={sendCommandMutation.isPending}
                  disabled={sendCommandMutation.isPending}>
                  <FormattedMessage id={cmd.labelId} />
                </Button>
              ))}
            </div>
          </ModalBody>
        </ModalContent>
        <ModalActions cancel={props.close}>
          <Button color="white" onClick={props.close}>
            <FormattedMessage id="generic.close" />
          </Button>
        </ModalActions>
      </ModalContainer>
    </Modal>
  );
}
